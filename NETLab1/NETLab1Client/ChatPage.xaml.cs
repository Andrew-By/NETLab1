using NETLab1;
using NETLab1.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
    /// Логика взаимодействия для ChatPage.xaml
    /// </summary>
    public partial class ChatPage : Page
    {
        public ChatPage()
        {
            InitializeComponent();
            App.Socket.TextMessageRecieved += Socket_TextMessageRecieved;
        }

        private ObservableCollection<TextMessage> _history = new ObservableCollection<TextMessage>();
        public ObservableCollection<TextMessage> History
        {
            get { return _history; }
            set
            {
                if(value!=_history)
                {
                    _history = value;
                    NotifyPropertyChanged("History");
                }
            }
        }

        private void Socket_TextMessageRecieved(object sender, TextMessage e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                History.Add(e);
            }));
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendMessage();
        }

        private void SendMessage()
        {
            TextMessage message = new TextMessage(MessageTextBox.Text, App.Socket.Nick);
            History.Add(message);
            MessageTextBox.Text = String.Empty;
            App.Socket.SendMessageAsync(message.Text);
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
