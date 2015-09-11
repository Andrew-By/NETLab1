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
            App.Current.Exit += Current_Exit;
            App.Socket.TextMessageRecieved += Socket_TextMessageRecieved;
            App.Socket.MessageDelivered += Socket_MessageDelivered;
            App.Socket.UserListUpdated += Socket_UserListUpdated;
            App.Socket.Kicked += Socket_Kicked;
            ChatRooms.Add(new ChatRoom());

            foreach (String user in App.Socket.UserList)
                ChatRooms[0].UserList.Add(user);
        }

        private void Socket_MessageDelivered(object sender, TextMessage e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    ChatRoom activeRoom = ChatRooms.FirstOrDefault(x => x.History.Any(y => y.Hash == e.Command.Value));
                    TextMessage message = activeRoom.History.FirstOrDefault(x => x.Hash == e.Command.Value);
                    message.Delivered = true;
                }));
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            App.Socket.SendMessageAsync("/exit" + MessageTextBox.Text);
            App.Socket.Close();
        }

        public String ServerName
        {
            get { return App.Socket.ServerName; }
        }

        public ObservableCollection<ChatRoom> _chatRooms = new ObservableCollection<ChatRoom>();
        public ObservableCollection<ChatRoom> ChatRooms
        {
            get
            {
                return _chatRooms;
            }
        }

        private void Socket_TextMessageRecieved(object sender, TextMessage e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ChatRoom activeRoom = ChatRooms[0];
                if (e.Command.Key.StartsWith("private"))
                {
                    activeRoom = ChatRooms.FirstOrDefault(x => x.Name == e.From);
                    if (activeRoom == null)
                    {
                        activeRoom = new ChatRoom(e.From);
                        ChatRooms.Add(activeRoom);
                    }
                    e.Text = e.Command.Value.Substring(e.Command.Value.IndexOf(' ') + 1);
                }
                activeRoom.History.Add(e);
            }));
        }

        private void Socket_UserListUpdated(object sender, List<string> e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ChatRooms[0].UserList.Clear();
                foreach (String user in e)
                    ChatRooms[0].UserList.Add(user);
            }));
        }

        private void Socket_Kicked(object sender, string e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageTextBox.IsEnabled = false;
                SendButton.IsEnabled = false;
                String message = "Вас исключили из чата.";
                if (e != String.Empty)
                    message += " Причина: " + e;
                MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
            ChatRoom activeRoom = ChatRooms[ChatRoomsTabControl.SelectedIndex];
            TextMessage message = null;
            if (activeRoom.IsPublic)
            {
                message = new TextMessage(MessageTextBox.Text, App.Socket.Nick);
                App.Socket.SendMessageAsync(message.Text);
            }
            else
            {
                message = new TextMessage(String.Format("/private {0} {1}", activeRoom.Name, MessageTextBox.Text), App.Socket.Nick);
                App.Socket.SendMessageAsync(message.Text);
                message.Text = message.Command.Value.Substring(message.Command.Value.IndexOf(' ') + 1);
            }
            MessageTextBox.Text = String.Empty;
            activeRoom.History.Add(message);
            if (message.Command.Key == "exit")
            {
                App.Current.Exit -= Current_Exit;
                App.Socket.Close();
                Application.Current.Shutdown();
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if ((sender as TextBlock).Text != App.Socket.Nick)
                {
                    ChatRoom privateRoom = new ChatRoom((sender as TextBlock).Text);
                    ChatRooms.Add(privateRoom);
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
