using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace NETLab1.Models
{
    public class ChatRoom : INotifyPropertyChanged
    {
        public ChatRoom(string name)
        {
            this.Name = name;
            this.IsPublic = false;
        }

        public ChatRoom()
        {
            this.Name = "Общий чат";
            this.IsPublic = true;
        }
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if(value!=_name)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private ObservableCollection<TextMessage> _history = new ObservableCollection<TextMessage>();
        public ObservableCollection<TextMessage> History
        {
            get { return _history; }
        }

        private ObservableCollection<String> _userList = new ObservableCollection<string>();
        public ObservableCollection<String> UserList
        {
            get { return _userList; }
        }

        private bool _isPublic;
        public bool IsPublic
        {
            get { return _isPublic; }
            set
            {
                if(value)
                {
                    _isPublic = value;
                    NotifyPropertyChanged("IsPublic");
                }
            }
        }
        public override string ToString()
        {
            return Name;
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
