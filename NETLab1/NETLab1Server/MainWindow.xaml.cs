using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NETLab1Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SocketPermission permission;
        Socket server;
        List<Socket> listeners;
        Thread serverTh;
        List<Thread> threads;
        const int port = 27000;
        const int buffSize = 2048;
        bool paused = false;
        ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        ManualResetEvent _pauseEvent = new ManualResetEvent(true);
        delegate void Listener(Socket sender);

        public MainWindow()
        {
            InitializeComponent();
            serverTh = new Thread(new ThreadStart(ServerThread));
        }

        private void ServerThread()
        {
            listeners = new List<Socket>();
            threads = new List<Thread>();
            permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", port);
            permission.Demand();
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Any, port));
            server.Blocking = true;
            while (true)
            {
                _pauseEvent.WaitOne(Timeout.Infinite);
                if (_shutdownEvent.WaitOne(0))
                    break;
                server.Listen(10);
                Socket temp = server.Accept();
                listeners.Add(temp);
                Thread th = new Thread(() => Listen(temp));
                threads.Add(th);
                th.Start();
            }
        }

        private void Listen(Socket sender)
        {
            byte[] buffer = new byte[buffSize];
            String data;
            while (true)
            {
                _pauseEvent.WaitOne(Timeout.Infinite);
                if (_shutdownEvent.WaitOne(0))
                    break;
                data = String.Empty;
                while (true)
                {
                    int bytesRec = sender.Receive(buffer);
                    data += Encoding.UTF8.GetString(buffer, 0, bytesRec);
                    if (data.ToString().IndexOf('\n') > -1)
                        break;
                }
                Thread.Sleep(100);
                Dispatcher.BeginInvoke(new Action(() => MessageArea.Text += "\n" + DateTime.Now.ToString() + " - " + data.TrimEnd('\n')));
                Thread.Sleep(100);
            }
        }

        private void Launch_Click(object sender, RoutedEventArgs e)
        {
            serverTh.Start();
            Launch.IsEnabled = false;
            Stop.IsEnabled = true;
            Pause.IsEnabled = true;
            MessageArea.Text += "\n" + DateTime.Now.ToString() + " - Сервер начал работу.";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _shutdownEvent.Set();
            _pauseEvent.Set();
            serverTh.Join();
            foreach (Thread th in threads) ///
                th.Join();
            Stop.IsEnabled = false;
            Pause.IsEnabled = false;
            MessageArea.Text += "\n" + DateTime.Now.ToString() + " - Сервер завершил работу.";
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            paused = !paused;
            if (paused)
            {
                _pauseEvent.Reset();
                Pause.Content = "Продолжить";
                MessageArea.Text += "\n" + DateTime.Now.ToString() + " - Сервер поставлен на паузу.";
            }
            else
            {
                _pauseEvent.Set();
                Pause.Content = "Пауза";
                MessageArea.Text += "\n" + DateTime.Now.ToString() + " - Сервер продолжил работу.";
            }
        }
    }
}
