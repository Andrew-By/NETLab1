using NETLab1.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace NETLab1Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _nick = "Admin";
        private const int _port = 4501;
        private const int _timeout = 2000;
        private const int _buffSize = 2048;
        SocketPermission permission;
        private Socket _server;
        private Thread _serverTh;
        private bool _paused = false;
        private TupleList<EndPoint, string> _receivers = new TupleList<EndPoint, string>();
        private ObservableCollection<TextMessage> _history = new ObservableCollection<TextMessage>();
        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        private ManualResetEvent _pauseEvent = new ManualResetEvent(true);

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
            _serverTh = new Thread(() => Listen());
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
                    if (!_receivers.Any(c => c.Item2.Equals(message.From)))
                    {
                        _receivers.Add(senderRemote, message.From);
                        Dispatcher.BeginInvoke(new Action(() => UserList.Items.Add(message.From)));
                    }
                    _server.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TextMessage("/confirmation " + message.Hash, _nick))), senderRemote);
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

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            ServerThread();
            LaunchButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            PauseButton.IsEnabled = true;
            SendAll(new TextMessage("Сервер запущен.", _nick));
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = false;
            PauseButton.IsEnabled = false;
            SendAll(new TextMessage("Сервер завершает работу.", _nick));
            _shutdownEvent.Set();
            _pauseEvent.Set();
            _serverTh.Join();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _paused = !_paused;
            if (_paused)
            {
                _pauseEvent.Reset();
                PauseButton.Content = "Возобновить";
                SendAll(new TextMessage("Сервер приостановлен.", _nick));
            }
            else
            {
                _pauseEvent.Set();
                PauseButton.Content = "Приостановить";
                SendAll(new TextMessage("Сервер возобновил работу.", _nick));
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
