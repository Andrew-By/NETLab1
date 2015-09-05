using System;
using System.Collections.Generic;
using System.Text;

namespace NETLab1.Models
{
    /// <summary>
    /// Сообщение, подтверждающее доставку
    /// </summary>
    public class ConfirmationMessage
    {
        public ConfirmationMessage(String messageHash, bool accepted)
        {
            this.MessageHash = messageHash;
            this.Accepted = accepted;
        }

        /// <summary>
        /// Хэш сообщения, о котором передаётся информация
        /// </summary>
        public string MessageHash { get; set; }

        /// <summary>
        /// Показывает, было ли сообщение корректно принято
        /// </summary>
        public bool Accepted { get; set; }

        public override string ToString()
        {
            return MessageHash.ToString();
        }
    }
}
