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
        public TextMessage(string text, string recipient)
        {
            this.Text = text;
            this.Recipient = recipient;
            this.SentTime = DateTime.Now;
        }

        public TextMessage(string text)
        {
            this.Text = text;
            this.SentTime = DateTime.Now;
        }

        /// <summary>
        /// Текстовое содержимое
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Ник получателя (или null, если всем)
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// Время отправки сообщения
        /// </summary>
        public DateTime SentTime { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
