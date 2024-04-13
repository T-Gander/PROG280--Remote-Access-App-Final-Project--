using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static PROG280__Remote_Access_App_Data__.Packet;

namespace PROG280__Remote_Access_App_Data__
{
    public class NetworkConnected
    {
        public bool IsConnected { get; set; } = false;

        protected const int _chunkSize = 1024;
        protected const int _packetSize = 1500;

        public TcpListener? TcpMessageListener { get; set; }
        public TcpListener? TcpVideoListener { get; set; }
        public TcpClient? TcpVideoClient { get; set; }
        public TcpClient? TcpMessageClient { get; set; }

        protected NetworkStream? _videoStream { get; set; }

        protected NetworkStream? _messageStream { get; set; }

        public static ObservableCollection<string> LogMessages { get; set; } = new();

        public static ObservableCollection<string> ChatMessages {  get; set; } = new();

        public static Task AddToLogMessagesList(string message)
        {
            LogMessages.Add(message);
            return Task.CompletedTask;
        }

        public void CloseConnections()
        {
            _messageStream?.Close();
            _messageStream?.Dispose();
            _videoStream?.Close();
            TcpVideoClient?.Close();
            _videoStream?.Dispose();
            TcpVideoClient?.Dispose();
        }

        public void ShutDown()
        {
            TcpVideoListener?.Stop();
            TcpVideoListener?.Dispose();
        }

        public async Task InitializeMessaging(string remoteip, int messagePort)
        {
            TcpMessageListener = new(IPAddress.Any, messagePort);
            await Task.Delay(1000);
            TcpMessageClient =  new(remoteip, messagePort);
        }

        public async Task ReceiveMessages()
        {
            try
            {
                _messageStream = TcpMessageClient!.GetStream();

                while (true)
                {
                    byte[] buffer = new byte[_packetSize];
                    int bytesRead = await _messageStream!.ReadAsync(buffer, 0, buffer.Length);
                    var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var packet = JsonConvert.DeserializeObject<Packet>(stringMessage);

                    if (packet!.ContentType == Packet.MessageType.Message)
                        ChatMessages.Add(packet.Payload!);
                }
            }
            catch (Exception ex)
            {
                //Something bad happened
            }
        }

        public async Task SendMessage(string message)
        {
            try
            {
                _messageStream = TcpMessageClient!.GetStream();

                Packet messagePacket = new Packet()
                {
                    ContentType = MessageType.Message,
                    Payload = message
                };

                byte[] initialpacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messagePacket));

                await _messageStream.WriteAsync(initialpacket, 0, initialpacket.Length);
            }
            catch (Exception ex)
            {
                //Something bad happened
            }
        }
    }
}
