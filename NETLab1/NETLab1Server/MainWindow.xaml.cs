﻿using NETLab1.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NETLab1Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _nick = "Admin";
        private const int _port = 4501;
        private const int _timeout = 800;
        private const int _buffSize = 2048;
        private SocketPermission permission;
        private Socket _server;
        private Thread _serverTh;
        private bool _paused = false;
        private TupleList<EndPoint, string> _receivers = new TupleList<EndPoint, string>();
        private ObservableCollection<String> _userlist = new ObservableCollection<String>();
        private ObservableCollection<TextMessage> _history = new ObservableCollection<TextMessage>();
        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        private ManualResetEvent _pauseEvent = new ManualResetEvent(true);
        private DispatcherTimer _timer = new DispatcherTimer();
        private DateTime _startTime = new DateTime();
        private TimeSpan _offset = new TimeSpan(), _offsetG = new TimeSpan();

        public class TupleList<T1, T2> : List<Tuple<T1, T2>>
        {
            public void Add(T1 item, T2 item2)
            {
                Add(new Tuple<T1, T2>(item, item2));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ServerThread()
        {
            permission = new SocketPermission(NetworkAccess.Accept, TransportType.Udp, "", _port);
            permission.Demand();
            _server = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            _server.Bind(new IPEndPoint(IPAddress.IPv6Any, _port));
            _server.Blocking = true;
            _serverTh = new Thread(Listen);
            _serverTh.Start();
        }

        private void Listen()
        {
            int bytesRec;
            byte[] buffer = new byte[_buffSize];
            String data;

            IPEndPoint sender = new IPEndPoint(IPAddress.IPv6Any, 0);
            EndPoint senderRemote = (EndPoint)sender;
            _server.ReceiveTimeout = _timeout;

            while (true)
            {
                _pauseEvent.WaitOne(Timeout.Infinite);
                if (_shutdownEvent.WaitOne(0))
                    break;
                data = String.Empty;
                try
                {
                    while (true)
                    {
                        try
                        {
                            bytesRec = _server.ReceiveFrom(buffer, ref senderRemote);
                            data += Encoding.UTF8.GetString(buffer, 0, bytesRec);
                            if (data.ToString().IndexOf('}') > -1)
                                break;
                        }
                        catch (SocketException)
                        {
                            if (_shutdownEvent.WaitOne(0))
                                break;
                        }
                    }
                }
                catch (SocketException) { }
                TextMessage message = JsonConvert.DeserializeObject(data, typeof(TextMessage)) as TextMessage;
                if (message != null)
                {
                    _server.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TextMessage("/confirmation " + message.Hash, _nick))), senderRemote);
                    if (!_receivers.Any(c => c.Item2.Equals(message.From)))
                    {
                        _receivers.Add(senderRemote, message.From);
                        Dispatcher.BeginInvoke(new Action(() => UserList.Add(message.From))).Wait();
                        foreach (var receiver in _receivers)
                        {
                            _server.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TextMessage("/userlist " + JsonConvert.SerializeObject(UserList), _nick))), receiver.Item1);
                        }
                    }
                    if (message.Command.Key == "message")
                    {
                        Dispatcher.BeginInvoke(new Action(() => History.Add(message)));
                        var s = _receivers.FirstOrDefault(x => x.Item2 == message.From);
                        foreach (var receiver in _receivers)
                        {
                            if (receiver != s)
                                SendMessage(message, receiver.Item1);
                        }
                    }
                }
            }
        }

        private void SendMessage(TextMessage message, EndPoint receiver)
        {
            _server.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), receiver);
        }

        private void SendAll(TextMessage message)
        {
            History.Add(message);
            foreach (var receiver in _receivers)
                SendMessage(message, receiver.Item1);
        }

        private string GetMessage()
        {
            string message = MessageTextBox.Text;
            MessageTextBox.Text = String.Empty;
            return message;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendAll(new TextMessage(GetMessage(), _nick));
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendAll(new TextMessage(GetMessage(), _nick));
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            var runtime = DateTime.Now - _startTime - _offsetG;
            UpTime.Text = "Время работы сервера: " + runtime.ToString(@"hh\:mm\:ss");
            CommandManager.InvalidateRequerySuggested();
        }


        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            _startTime = DateTime.Now;
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.Interval = new TimeSpan(0, 0, 1);
            _timer.Start();
            ServerThread();
            LaunchButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            PauseButton.IsEnabled = true;
            SendAll(new TextMessage("Сервер запущен.", _nick));
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _paused = !_paused;
            if (_paused)
            {
                _offset = new TimeSpan(DateTime.Now.Ticks);
                _pauseEvent.Reset();
                PauseButton.Content = "Возобновить";
                SendAll(new TextMessage("Сервер приостановлен.", _nick));
                _timer.Stop();
            }
            else
            {
                _offset = new TimeSpan(DateTime.Now.Ticks - _offset.Ticks);
                _offsetG += _offset;
                _timer.Start();
                _pauseEvent.Set();
                PauseButton.Content = "Приостановить";
                SendAll(new TextMessage("Сервер возобновил работу.", _nick));
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = false;
            PauseButton.IsEnabled = false;
            SendAll(new TextMessage("Сервер завершает работу.", _nick));
            _shutdownEvent.Set();
            _pauseEvent.Set();
            _serverTh.Join();
            _timer.Stop();
        }

        private void KickButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserListView.SelectedIndex > -1)
            {
                var user = _receivers[UserListView.SelectedIndex];
                String message = GetMessage();
                _server.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TextMessage("/kick " + message, _nick))), user.Item1);
                _receivers.Remove(user);
                UserList.Remove(user.Item2);
                String textMessage = String.Format("Пользователь {0} был исключён из чата.", user.Item2);
                if (message != String.Empty)
                    textMessage += " Причина: " + message;
                SendAll(new TextMessage(textMessage, _nick));
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Лабораторная работа №1 - сервер-клиентское приложение-чат на основе блокирующих сокетов протокола UDP.\nВыполнили студенты группы МП-45: Бычков А. и Еленский И.", "Разработчики", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        public ObservableCollection<String> UserList
        {
            get { return _userlist; }
            set
            {
                if (value != _userlist)
                {
                    _userlist = value;
                    NotifyPropertyChanged("UserList");
                }
            }
        }

        public ObservableCollection<TextMessage> History
        {
            get { return _history; }
            set
            {
                if (value != _history)
                {
                    _history = value;
                    NotifyPropertyChanged("History");
                }
            }
        }

        #region INotify
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
