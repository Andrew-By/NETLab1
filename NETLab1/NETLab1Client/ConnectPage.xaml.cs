using NETLab1.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

namespace NETLab1Client
{
    /// <summary>
    /// Логика взаимодействия для ConnectPage.xaml
    /// </summary>
    public partial class ConnectPage : Page
    {
        private bool EnableValidation = false;

        public ConnectPage()
        {
            InitializeComponent();
            EnableValidation = true;
            ValidateInput();
        }

        private void ValidateInput()
        {
            if (EnableValidation)
            {
                if (ServerTextBox.Text.Length > 0 &&
                PortTextBox.Text.Length > 0 &&
                NickTextBox.Text.Length > 0)
                    ConnectButton.IsEnabled = true;
                else
                    ConnectButton.IsEnabled = false;
            }
        }

        private void ServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInput();
        }

        private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInput();
        }

        private void NickTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInput();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
                IPHostEntry hostEntry = Dns.GetHostEntry(ServerTextBox.Text);
                IPEndPoint endPoint = new IPEndPoint(hostEntry.AddressList[0], int.Parse(PortTextBox.Text));
                socket.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new TextMessage("/nick " + NickTextBox.Text))), endPoint);
                NavigationService.Navigate(new Uri("ChatPage.xaml", UriKind.Relative));
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
