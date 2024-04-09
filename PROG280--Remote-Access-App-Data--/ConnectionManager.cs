using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Converters;
using static PROG280__Remote_Access_App_Data__.Packet;
using static System.Net.Mime.MediaTypeNames;

namespace PROG280__Remote_Access_App_Data__
{
    public class ConnectionManager
    {
        private bool _isConnected = false;
        private int _port = 8000;

        //private RemoteWindow _remoteWindow;

        private string _localIPAddress;

        private string _remoteIPAddress;

        public string IPAddress 
        { 
            get
            {
                switch (_isServer)
                {
                    case true:
                        return _localIPAddress;

                    case false:
                        return _remoteIPAddress;
                }
            }
            set 
            {
                switch (_isServer)
                {
                    case true:
                        _localIPAddress = value;
                        break;
                    case false:
                        _remoteIPAddress = value;
                        break;
                }
            }
        }

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
                _tcpClient = value;
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

        //public Task<Packet> Receive()
        //{
        //    switch (_isServer)
        //    {
        //        case true:
        //            return ServerReceive();

        //        case false:
        //            return ClientReceive();
        //    }
        //}

        //private async Task<Packet?> ServerReceive()
        //{
        //    NetworkStream stream = TcpClient!.GetStream();
        //    while (IsConnected)
        //    {
        //        byte[] buffer = new byte[4096];
        //        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        //        var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        //        var packet = JsonConvert.DeserializeObject<Packet>(stringMessage);
        //        if (packet != null)
        //            return packet;
        //    }
        //    return null;
        //}

        //public async Task ServerSend(Packet packet)
        //{
        //    NetworkStream stream = TcpClient!.GetStream();
        //    while (IsConnected)
        //    {
        //        byte[] buffer = new byte[4096];
        //        var sendpacket = JsonConvert.SerializeObject(packet);
        //        var packetbytes = Encoding.UTF8.GetBytes(sendpacket);
        //        await stream.WriteAsync(packetbytes, 0, buffer.Length);
        //    }
        //}

        public async Task Send()
        {
            switch (_isServer)
            {
                case true:
                    //Method to grab screen
                    //Create packet
                    //Send packet to client.
                    byte[] bitmapBytes;

                    Bitmap screen = GrabScreen();

                    using (MemoryStream mstream = new())
                    {
                        screen.Save(mstream, ImageFormat.Png);
                        bitmapBytes = mstream.ToArray();

                        int chunkSize = 1024;
                        int totalChunks = (int)Math.Ceiling((double)bitmapBytes.Length / chunkSize) + 1;    //+ 1 is for the initial chunk size packet.

                        Packet firstPacket = new Packet()
                        {
                            ContentType = MessageType.Frame,
                            Payload = JsonConvert.SerializeObject(totalChunks)
                        };

                        byte[] initialpacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(firstPacket));

                        NetworkStream stream = TcpClient!.GetStream();
                        int bytesRead = Task.Run(async () => await stream.ReadAsync(new byte[1024], 0, 1024)).Result;
                        
                        for (int i = 0; i < totalChunks - 1; i++)
                        {
                            await stream.ReadAsync(new byte[chunkSize]);

                            Packet screenPacket = new();
                            screenPacket.ContentType = MessageType.Frame;

                            // Determine chunk boundaries
                            int offset = i * chunkSize;
                            int length = Math.Min(chunkSize, bitmapBytes.Length - offset);
                            byte[] chunk = new byte[length];
                            Buffer.BlockCopy(bitmapBytes, offset, chunk, 0, length);

                            screenPacket.Payload = JsonConvert.SerializeObject(chunk);

                            // Serialize packet and send
                            byte[] bytepacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(screenPacket));
                            await TcpClient.GetStream().WriteAsync(bytepacket, 0, bytepacket.Length);
                        }
                    }
                    screen.Dispose();
                    break;

                case false:

                    await SendAcknowledgementPacket();
                    break;
            }
        }

        private async Task SendAcknowledgementPacket()
        {
            NetworkStream stream = TcpClient!.GetStream();

            Packet ack = new Packet()
            {
                ContentType = MessageType.Acknowledgement
            };

            byte[] packetbytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ack)); ;

            await stream.WriteAsync(packetbytes, 0, packetbytes.Length);
        }
        
        public Bitmap GrabScreen()
        {
            Bitmap screenshot = new((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
            }

            return screenshot;
        }

        //private async Task<Packet> ClientReceive()
        //{
        //    return Receive().Result;
        //}
    }
}
