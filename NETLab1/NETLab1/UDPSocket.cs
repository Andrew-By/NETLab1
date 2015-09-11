using NETLab1.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        /// <summary>
        /// Источник токенов для прерывания прослушивания вхожящих сообщений
        /// </summary>
        private CancellationTokenSource _listenCT;

        /// <summary>
        /// Список сообщений, ожидающих доставки c источниками токенов отмены
        /// </summary>
        private Dictionary<String, CancellationTokenSource> _pendingDelivery;

        private String _serverName;
        public String ServerName
        {
            get { return _serverName; }
        }

        private String _nick;
        /// <summary>
        /// Ник пользователя
        /// </summary>
        public String Nick
        {
            get { return _nick; }
        }

        private List<String> _userList;
        public List<String> UserList
        {
            get { return _userList; }
        }

        /// <summary>
        /// Событие получения текстового сообщения
        /// </summary>
        public event EventHandler<TextMessage> TextMessageRecieved;
        public event EventHandler<TextMessage> MessageDelivered;
        public event EventHandler<TextMessage> DeliveryFailed;
        public event EventHandler<List<String>> UserListUpdated;
        public event EventHandler<String> Kicked;

        public UDPSocket(String toServer, int toPort, string nick)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(toServer);
            _serverName = hostEntry.HostName;
            _socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            _socket.ReceiveTimeout = MAX_RETRY_TIMEOUT;
            _endPoint = new IPEndPoint(hostEntry.AddressList[0], toPort);
            _nick = nick;
            _userList = new List<string>();
            _listenCT = new CancellationTokenSource();
            _pendingDelivery = new Dictionary<String, CancellationTokenSource>();
            CancellationToken ct = _listenCT.Token;
            Task.Run(() => Listen(ct));
        }

        /// <summary>
        /// Отправка сообщения по протоколу UDP
        /// </summary>
        /// <param name="toServer">Адрес или имя сервера</param>
        /// <param name="toPort">Порт</param>
        /// <param name="text">Текст сообщения</param>
        public void SendMessageAsync(String text)
        {
            TextMessage message = new TextMessage(text, Nick);
            CancellationTokenSource cts = new CancellationTokenSource();
            _pendingDelivery.Add(message.Hash, cts);
            Task.Run(() => DeliverMessageAsync(message, cts.Token));
        }

        private void DeliverMessageAsync(TextMessage message, CancellationToken ct)
        {
            for (int i = 0; i < MAX_RETRY_COUNT; i++)
            {
                if (ct.IsCancellationRequested)
                    break;

                Debug.WriteLine("Попытка {0} из {1}...", i + 1, MAX_RETRY_COUNT);
                _socket.SendTo(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), _endPoint);
                Debug.WriteLine("Сообщение {0} отправлено, ожидается подтверждение...", message.Hash);
                Thread.Sleep(MAX_RETRY_TIMEOUT);
            }

            if (!ct.IsCancellationRequested)
            {
                if (DeliveryFailed != null) DeliveryFailed(this, message);
                Debug.WriteLine("Не удалось доставить сообщение {0}!", message.Hash);
                _pendingDelivery.Remove(message.Hash);
            }
        }

        private void Listen(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                byte[] buffer = new byte[MAX_BUFF_SIZE];
                StringBuilder textBuffer = new StringBuilder();
                while (textBuffer.ToString().IndexOf('}') == -1 && !ct.IsCancellationRequested)
                {
                    try
                    {
                        int byteCount = _socket.Receive(buffer);
                        Debug.WriteLine("Получено {0} байт", byteCount);
                        textBuffer.Append(Encoding.UTF8.GetString(buffer));
                    }
                    catch
                    {
                        textBuffer.Clear();
                    }
                }

                Debug.WriteLine("Получен символ конца объекта, десериализация...");
                try
                {
                    TextMessage message = JsonConvert.DeserializeObject(textBuffer.ToString(), typeof(TextMessage)) as TextMessage;
                    message.Delivered = true;
                    switch (message.Command.Key)
                    {
                        case "confirmation":
                            Debug.WriteLine("Получено подтверждение!");
                            if (_pendingDelivery.ContainsKey(message.Command.Value))
                            {
                                _pendingDelivery[message.Command.Value].Cancel();
                                Debug.WriteLine("Сообщение {0} доставлено!", message.Command.Value);
                                if (MessageDelivered != null) MessageDelivered(this, message);
                            }
                            break;
                        case "message":
                        case "private":
                            Debug.WriteLine("Получено текстовое сообщение!");
                            if (TextMessageRecieved != null) TextMessageRecieved(this, message);
                            break;
                        case "userlist":
                            Debug.WriteLine("Получен список пользователей!");
                            _userList = JsonConvert.DeserializeObject(message.Command.Value, typeof(List<String>)) as List<String>;
                            if (UserListUpdated != null) UserListUpdated(this, _userList);
                            break;
                        case "kick":
                            Debug.WriteLine("Вас выгнали!");
                            if (Kicked != null) Kicked(this, message.Command.Value);
                            break;
                        default:
                            Debug.WriteLine("Получено сообщение неизвестного типа ({0})! Сообщение проигнорировано", message.Command.Key);
                            break;
                    }
                }
                catch
                {
                    Debug.WriteLine("Получен объект неизвестного типа! Сообщение проигнорировано");
                }
            }
        }

        public void Close()
        {
            Debug.WriteLine("Отмена сообщений, ожидающих доставки...");
            foreach (CancellationTokenSource cts in _pendingDelivery.Values)
                cts.Cancel();

            Debug.WriteLine("Отмена ожидания входящих сообщений...");
            if (_listenCT != null)
                _listenCT.Cancel();

            Debug.WriteLine("Закрытие сокета...");
            _socket.Close();
        }
    }
}
