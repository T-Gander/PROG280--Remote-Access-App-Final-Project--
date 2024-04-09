using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static PROG280__Remote_Access_App_Data__.Packet;

namespace PROG280__Remote_Access_App_Data__
{
    public class ConnectionManager
    {
        private bool _isConnected = false;
        private int _port = 8000;

        public long RemoteIPAddress { get; set; }

        public long LocalIPAddress { get; set; }

        private TcpListener? _tcpListener;
        private TcpClient? _tcpClient;

        public TcpListener? TcpListener
        {
            get
            {
                return _tcpListener;
            }
            set
            {
                _tcpListener = value;
                _isServer = true;
            }
        }

        private bool _isServer = false;

        public TcpClient? TcpClient
        {
            get
            {
                return _tcpClient;
            }
            set
            {
                if (_tcpClient != null)
                {
                    _tcpClient = value;
                    _isServer = false;
                    IsConnected = true;
                }
                else
                {
                    _tcpClient = value;
                    IsConnected = false;
                }
            }
        }

        private ObservableCollection<string> _messages = new ObservableCollection<string>();

        public ObservableCollection<string> Messages { get { return _messages; } }

        public void AddToMessagesList(string message)
        {
            _messages.Add(message);
        }

        public int Port
        {
            get { return _port; }
            set
            {
                if (int.TryParse(value.ToString(), out int parsedValue))
                {
                    _port = parsedValue;
                }
                else
                {
                    _port = 0;
                };
            }
        }

        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                }
            }
        }

        public Task<Packet> Receive()
        {
            switch (_isServer)
            {
                case true:
                    return ServerReceive();

                case false:
                    return ClientReceive();
            }
        }

        private async Task<Packet> ServerReceive()
        {
            NetworkStream stream = TcpClient!.GetStream();
            while (IsConnected)
            {
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var packet = JsonConvert.DeserializeObject<Packet>(stringMessage);
                if (packet != null)
                    return packet;
            }
            return null;
        }

        private async Task<Packet> ClientReceive()
        {
            return new Packet();
        }
    }
}
