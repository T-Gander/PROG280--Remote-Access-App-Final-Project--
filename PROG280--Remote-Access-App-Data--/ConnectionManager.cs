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
        private const int _chunkSize = 1024;
        private const int _packetSize = 1500;
        private int _port = 9000;

        public bool IsServer { get; set; } = false;

        private NetworkStream? _videoStream { get; set; } = TcpVideoClient!.GetStream();

        public string LocalIPAddress { get; set; } = string.Empty;

        public string RemoteIPAddress { get; set; } = string.Empty;

        public static TcpListener? TcpListener { get; set; }
        public static TcpClient? TcpVideoClient { get; set; }

        public ObservableCollection<string> Messages { get; set; } = new();

        public void AddToMessagesList(string message)
        {
            Messages.Add(message);
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

        public bool IsConnected { get; set; } = false;

        private async Task SendAcknowledgementPacket(NetworkStream stream)
        {
            Packet ack = new Packet()
            {
                ContentType = MessageType.Acknowledgement
            };

            byte[] packetbytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ack)); ;

            await stream.WriteAsync(packetbytes, 0, packetbytes.Length);
        }

        private async Task<MessageType> ReceiveVideoAckPacket()
        {
            byte[] buffer = new byte[_packetSize];
            int bytesRead = await _videoStream.ReadAsync(buffer, 0, buffer.Length);
            var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var packet = JsonConvert.DeserializeObject<Packet>(stringMessage);

            return packet!.ContentType;
        }

        private async Task SendVideoAckPacket()
        {
            Packet ackPacket = new Packet()
            {
                ContentType = MessageType.Acknowledgement
            };

            byte[] ackBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ackPacket));

            await _videoStream.WriteAsync(ackBytes, 0, ackBytes.Length);
        }
        
        private Bitmap GrabScreen()
        {
            Bitmap screenshot = new((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
            }

            return screenshot;
        }

        private async Task<int> SendFrameStartPacket(byte[] bitmapBytes)
        {
            int totalChunks = (int)Math.Ceiling((double)bitmapBytes.Length / _chunkSize);

            Packet frameStartPacket = new Packet()
            {
                ContentType = MessageType.FrameStart,
                Payload = JsonConvert.SerializeObject(totalChunks)
            };

            byte[] initialpacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(frameStartPacket));
            
            await _videoStream.WriteAsync(initialpacket, 0, initialpacket.Length);

            return totalChunks;
        }

        private async Task SendFrameEndPacket()
        {
            Packet frameEndPacket = new Packet()
            {
                ContentType = MessageType.FrameEnd,
                Payload = ""
            };

            byte[] endPacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(frameEndPacket));

            await _videoStream.WriteAsync(endPacket, 0, endPacket.Length);
        }

        public async Task SendVideoPackets()
        {
            try
            {
                byte[] bitmapBytes;
                Bitmap screen = GrabScreen();

                using (MemoryStream mstream = new())
                {
                    screen.Save(mstream, ImageFormat.Png);
                    bitmapBytes = mstream.ToArray();

                    int totalChunks = await SendFrameStartPacket(bitmapBytes);

                    int chunkIndex = 0;

                    while (chunkIndex < totalChunks)
                    {
                        //Wait for acknowledgement packet
                        if (await ReceiveVideoAckPacket() != MessageType.Acknowledgement)
                        {
                            throw new Exception();
                        }

                        await SendVideoFrameChunk(chunkIndex, bitmapBytes);

                        chunkIndex++;
                    }

                    await SendFrameEndPacket();
                }
                screen.Dispose();
            }
            catch (Exception ex) 
            {
                AddToMessagesList(ex.Message);
            }
        }

        private async Task SendVideoFrameChunk(int chunkIndex, byte[] bitmapBytes)
        {
            Packet screenPacket = new();
            screenPacket.ContentType = MessageType.Frame;

            // Determine chunk boundaries
            int offset = chunkIndex * _chunkSize;
            int length = Math.Min(_chunkSize, bitmapBytes.Length - offset);
            byte[] chunk = new byte[_chunkSize];
            Buffer.BlockCopy(bitmapBytes, offset, chunk, 0, length);

            screenPacket.Payload = JsonConvert.SerializeObject(chunk);

            // Serialize packet and send
            byte[] bytepacket = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(screenPacket));
            await TcpVideoClient!.GetStream().WriteAsync(bytepacket, 0, bytepacket.Length);
        }

        public async Task<Bitmap> ReceiveVideoPackets()
        {
            List<byte> frameChunks = new List<byte>();

            int expectedChunks = 0;

            while (IsConnected)
            {
                byte[] buffer = new byte[_packetSize];
                int bytesRead = await _videoStream.ReadAsync(buffer, 0, buffer.Length);
                var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var packet = JsonConvert.DeserializeObject<Packet>(stringMessage);

                if(packet!.ContentType != MessageType.Frame)
                {
                    switch(packet.ContentType)
                    {
                        case MessageType.FrameStart:
                            expectedChunks = JsonConvert.DeserializeObject<int>(packet.Payload!);
                            continue;

                        default:
                            break;
                    }
                }
                else
                {
                    frameChunks.AddRange(JsonConvert.DeserializeObject<byte[]>(packet.Payload!)!);
                    await SendVideoAckPacket();
                }
            }

            if(frameChunks.Count/_chunkSize != expectedChunks)
            {
                //Something bad happened if you end up here.
            }

            byte[] bitmapBytes = frameChunks.ToArray();

            Bitmap frame;

            using (MemoryStream mstream = new(bitmapBytes))
            {
                frame = new Bitmap(mstream);
            }
            return frame;
        }
    }
}
