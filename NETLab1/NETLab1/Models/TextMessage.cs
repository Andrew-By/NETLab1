using Newtonsoft.Json;
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
        [JsonConstructor]
        public TextMessage(string text, string to, string from)
        {
            this.Text = text;
            this.To = to;
            this.From = from;
            this.SentTime = DateTime.Now;
        }

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
        /// Ник получателя (или null, если всем)
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Ник отправителя
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Время отправки сообщения
        /// </summary>
        public DateTime SentTime { get; set; }

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
