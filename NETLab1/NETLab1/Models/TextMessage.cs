using System;
using System.Collections.Generic;
using System.Text;

namespace NETLab1.Models
{
    /// <summary>
    /// Класс сообщения чата
    /// </summary>
    public class TextMessage
    {

        public TextMessage(string text, string from)
        {
            this.Text = text;
            this.From = from;
            this.SentTime = DateTime.Now;
        }

        /// <summary>
        /// Текстовое содержимое
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Ник отправителя
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Время отправки сообщения
        /// </summary>
        public DateTime SentTime { get; set; }

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

        /// <summary>
        /// Контрольная сумма сообщения
        /// </summary>
        public string Hash
        {
            get { return Convert.ToBase64String(Encoding.UTF8.GetBytes(Text)); }
        }
        public override string ToString()
        {
            return Text;
        }
    }
}
