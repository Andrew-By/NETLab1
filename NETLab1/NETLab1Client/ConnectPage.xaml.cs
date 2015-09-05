﻿using NETLab1;
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
            ConnectingProgressBar.Visibility = Visibility.Hidden;
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

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ServerTextBox.IsEnabled = false;
            PortTextBox.IsEnabled = false;
            NickTextBox.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            ConnectingProgressBar.Visibility = Visibility.Visible;

            UDPSocket socket = new UDPSocket();
            try
            {
                await socket.SendMessageAsync(ServerTextBox.Text, int.Parse(PortTextBox.Text), "/nick " + NickTextBox.Text);
                NavigationService.Navigate(new Uri("ChatPage.xaml", UriKind.Relative));
            }
            catch(Exception ex)
            {
                ServerTextBox.IsEnabled = true;
                PortTextBox.IsEnabled = true;
                NickTextBox.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                ConnectingProgressBar.Visibility = Visibility.Hidden;
                MessageBox.Show("Не удалось подключиться к серверу", "Произошла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
