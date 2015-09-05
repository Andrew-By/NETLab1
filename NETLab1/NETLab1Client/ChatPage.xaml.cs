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

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            TextMessage message = new TextMessage(MessageTextBox.Text, App.Socket.Nick);
            History.Add(message);
            MessageTextBox.Text = String.Empty;
            await App.Socket.SendMessageAsync(message.Text);
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
