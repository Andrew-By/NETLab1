using NETLab1.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETLab1
{
    /// <summary>
    /// Класс обеспечения работы с сокетом в режиме UDP, с подтверждением доставки
    /// </summary>
    public class UDPSocket
    {
        /// <summary>
        /// Количество попыток отправить сообщение
        /// </summary>
        private const int MAX_RETRY_COUNT = 5;

        /// <summary>
        /// Время ожидания до повторной отправки
        /// </summary>
        private const int MAX_RETRY_TIMEOUT = 5000;

        /// <summary>
        /// Максимально допустимый объём буффера чтения
        /// </summary>
        private const int MAX_BUFF_SIZE = 2048;

        /// <summary>
        /// Сокет, с помощью устанавливаются все соединения
        /// </summary>
        private Socket _socket;

        /// <summary>
        /// Адрес удалённого компьютера
        /// </summary>
        private IPEndPoint _endPoint;

        private String _nick;
        /// <summary>
        /// Ник пользователя
        /// </summary>
        public String Nick
        {
            get { return _nick; }
        }
        public UDPSocket(String toServer, int toPort, string nick)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(toServer);
            _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            _endPoint = new IPEndPoint(hostEntry.AddressList[0], toPort);
            _nick = nick;
        }

        /// <summary>
        /// Отправка сообщения по протоколу UDP
        /// </summary>
        /// <param name="toServer">Адрес или имя сервера</param>
        /// <param name="toPort">Порт</param>
        /// <param name="text">Текст сообщения</param>
        public async Task SendMessageAsync(String text)
        {
            try
            {
                TextMessage message = new TextMessage(text, _nick);
                Debug.WriteLine("Отправка сообщения {0}...", message.Hash);

                await DeliverMessageAsync(message);
            }
            catch
            {
                throw;
            }
        }

        private async Task DeliverMessageAsync(TextMessage message)
        {
            bool delivered = false;

            await Task.Run(() =>
            {
                for (int i = 0; i < MAX_RETRY_COUNT; i++)
                {
                    Debug.WriteLine("Попытка {0} из {1}...", i + 1, MAX_RETRY_COUNT);
                    _socket.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), _endPoint);
                    Debug.WriteLine("Сообщение {0} отправлено, ожидается подтверждение...", message.Hash);

                    Task confirmation = new Task(() => WaitForConfirmation(_socket, message.Hash));
                    confirmation.Start();
                    if (confirmation.Wait(MAX_RETRY_TIMEOUT))
                    {
                        Debug.WriteLine("Сообщение {0} доставлено!", message.Hash);
                        delivered = true;
                        break;
                    }
                    Debug.WriteLine("Превышен интервал ожидания доставки!");
                }
            });

            if(!delivered)
                throw new Exception("Превышен интервал ожидания доставки!");
        }

        private void WaitForConfirmation(Socket socket, String messageHash)
        {
            byte[] buffer = new byte[MAX_BUFF_SIZE];
            StringBuilder textBuffer = new StringBuilder();
            ConfirmationMessage cmessage = null;

            do
            {
                do
                {
                    try
                    {
                        socket.Receive(buffer);
                        textBuffer.Append(Encoding.UTF8.GetString(buffer));
                    }
                    catch
                    {
                        break;
                        throw;
                    }
                }
                while (textBuffer.ToString().IndexOf('}') == -1);
                try
                {
                    cmessage = JsonConvert.DeserializeObject(textBuffer.ToString(), typeof(ConfirmationMessage)) as ConfirmationMessage;
                }
                catch
                {
                    cmessage = null;
                }
            }
            while (cmessage == null || !(cmessage.MessageHash == messageHash && cmessage.Accepted));
        }

        public void Close()
        {
            _socket.Close();
        }
    }
}
