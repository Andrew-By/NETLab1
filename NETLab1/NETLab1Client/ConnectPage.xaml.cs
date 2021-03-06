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

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ServerTextBox.IsEnabled = false;
            PortTextBox.IsEnabled = false;
            NickTextBox.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            ConnectingProgressBar.Visibility = Visibility.Visible;

            App.Socket = new UDPSocket(ServerTextBox.Text, int.Parse(PortTextBox.Text), NickTextBox.Text);

            App.Socket.SendMessageAsync("/nick " + NickTextBox.Text);
            App.Socket.MessageDelivered += Socket_MessageDelivered;
            App.Socket.DeliveryFailed += Socket_DeliveryFailed;
            App.Socket.Error += Socket_Error;
            App.Current.Exit += Current_Exit;


        }

        private void Socket_Error(object sender, string e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ServerTextBox.IsEnabled = true;
                PortTextBox.IsEnabled = true;
                NickTextBox.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                ConnectingProgressBar.Visibility = Visibility.Hidden;

                App.Socket.MessageDelivered -= Socket_MessageDelivered;
                App.Socket.DeliveryFailed -= Socket_DeliveryFailed;
                App.Socket.Error -= Socket_Error;
                App.Current.Exit -= Current_Exit;
                MessageBox.Show(e, "Произошла ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }));
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            App.Socket.Close();
        }

        private void Socket_DeliveryFailed(object sender, TextMessage e)
        {
            Socket_Error(this, "Не удалось подключиться к серверу");
        }

        private void Socket_MessageDelivered(object sender, TextMessage e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                App.Socket.MessageDelivered -= Socket_MessageDelivered;
                App.Socket.DeliveryFailed -= Socket_DeliveryFailed;
                App.Socket.Error -= Socket_Error;
                App.Current.Exit -= Current_Exit;
                NavigationService.Navigate(new Uri("ChatPage.xaml", UriKind.Relative));
            }));
        }

        private void NickTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
                ConnectButton_Click(this, new RoutedEventArgs());
        }
    }
}
