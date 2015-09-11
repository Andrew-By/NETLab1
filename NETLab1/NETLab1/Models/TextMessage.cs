using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace NETLab1.Models
{
    /// <summary>
    /// Класс сообщения чата
    /// </summary>
    public class TextMessage : INotifyPropertyChanged
    {

        public TextMessage(string text, string from)
        {
            Random rand = new Random();

            this.Text = text;
            this.From = from;
            this.SentTime = DateTime.Now;
            this.Delivered = false;
            this._hash = rand.Next().ToString();
        }

        private string _text;
        /// <summary>
        /// Текстовое содержимое
        /// </summary>
        public string Text
        {
            get { return _text; }
            set
            {
                if(value!=_text)
                {
                    _text = value;
                    NotifyPropertyChanged("Text");
                }
            }
        }

        private string _from;
        /// <summary>
        /// Ник отправителя
        /// </summary>
        public string From
        {
            get { return _from; }
            set
            {
                if (value != _from)
                {
                    _from = value;
                    NotifyPropertyChanged("From");
                }
            }
        }

        /// <summary>
        /// Время отправки сообщения
        /// </summary>
        /// 
        private DateTime _sentTime;
        public DateTime SentTime
        {
            get { return _sentTime; }
            set
            {
                if(value!=_sentTime)
                {
                    _sentTime = value;
                    NotifyPropertyChanged("SentTime");
                }
            }
        }

        /// <summary>
        /// Получает команду, отправленную в сообщение. Текстовое сообщение эквивалентно команде /message
        /// </summary>
        public KeyValuePair<String, String> Command
        {
            get
            {
                if (Text.IndexOf('/') == 0)
                {
                    String[] words = Text.Split(' ');
                    if (words.Length > 1)
                        return new KeyValuePair<string, string>(words[0].Substring(1), Text.Substring(words[0].Length + 1));
                    else
                        return new KeyValuePair<string, string>(words[0].Substring(1), String.Empty);
                }
                else
                {
                    return new KeyValuePair<string, string>("message", Text);
                }
            }
        }

        private string _hash;

        /// <summary>
        /// Контрольная сумма сообщения
        /// </summary>
        public string Hash
        {
            //get { return Convert.ToBase64String(Encoding.UTF8.GetBytes(Text)); }
            get { return _hash; }
        }

        private bool _delivered;
        public bool Delivered
        {
            get { return _delivered; }
            set
            {
                if(value!=_delivered)
                {
                    _delivered = value;
                    NotifyPropertyChanged("Delivered");
                }
            }
        }

        public override string ToString()
        {
            return Text;
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
