using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using static PROG280__Remote_Access_App_Data__.Packet;
using PROG280__Remote_Access_App_Data__;
using System.Runtime.CompilerServices;
using System.Net.Http;

namespace PROG280__Remote_Access_App_Data__
{
    public class NetworkConnected
    {
        public delegate void FrameDelegate(BitmapImage frame);
        public event FrameDelegate FrameHandler;

        public delegate void ChunkDelegate(byte[] data);
        public event ChunkDelegate ChunkHandler;
        public event ChunkDelegate FileChunkHandler;

        public delegate void ChatDelegate(string message);
        public event ChatDelegate ChatHandler;

        public BitmapImage? CurrentFrame { get; set; }

        public bool ReceivingFile = false;
        public string ReceivingFileName = "";
        

        public NetworkConnected()
        {
            ChunkHandler += HandleFrameChunks;
            FrameHandler += HandleFrames;
            FileChunkHandler += HandleFileChunks;
            ChatHandler += HandleChatMessages;
        }

        private List<byte> frameChunks = new List<byte>();

        private List<byte> fileChunks = new List<byte>();

        public bool IsConnected { get; set; } = false;

        protected const int _chunkSize = 1024;
        protected const int _packetSize = 1500;

        public string? DisplayName { get; set; } = "Lazy User";

        public TcpListener? TcpListenerData { get; set; }
        public TcpClient? TcpClientData { get; set; }

        protected NetworkStream? _dataStream { get; set; }

        public static ObservableCollection<string> LogMessages { get; set; } = new();

        public static ObservableCollection<string> ChatMessages {  get; set; } = new();

        public static Task AddToLogMessagesList(string message)
        {
            LogMessages.Add(message);
            return Task.CompletedTask;
        }

        public void CloseConnections()
        {
            //_messageStream?.Close();
            //_messageStream?.Dispose();
            //_videoStream?.Close();
            //TcpVideoClient?.Close();
            //_videoStream?.Dispose();
            //TcpVideoClient?.Dispose();
        }

        public static async Task<bool> ShowAcceptFilePopupAsync()
        {
            // Show a dialog box asking the user whether to accept the file transfer
            MessageBoxResult result = await Task.Run(() =>
                MessageBox.Show("Do you want to accept the file transfer?", "File Transfer", MessageBoxButton.YesNo));

            // Return true if the user clicked Yes, false otherwise
            return result == MessageBoxResult.Yes;
        }

        protected async Task SendMessageTypePacket(NetworkStream stream)
        {
            Packet ackPacket = new Packet()
            {
                ContentType = MessageType.Acknowledgement
            };

            byte[] ackBytes = new byte[_packetSize];

            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ackPacket)).CopyTo(ackBytes, 0);

            await stream.WriteAsync(ackBytes, 0, ackBytes.Length);
        }

        private void HandleFileChunks(byte[] chunk)
        {
            fileChunks.AddRange(chunk);
        }

        private void HandleFrameChunks(byte[] chunk)
        {
            frameChunks.AddRange(chunk);
        }

        private void HandleFrames(BitmapImage? frame)
        {
            CurrentFrame = frame;
        }

        private void HandleChatMessages(string message)
        {
            ChatMessages.Add($"{DisplayName}: {message}");
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

        public async Task SendPacket<T>(MessageType messageType, T data)
        {
            _dataStream = TcpClientData!.GetStream();

            byte[] fileBytes = new byte[_packetSize];

            Packet filePacket = new Packet()
            {
                ContentType = messageType,
                Payload = JsonConvert.SerializeObject(data)
            };

            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(filePacket)).CopyTo(fileBytes, 0);

            await _dataStream.WriteAsync(fileBytes, 0, fileBytes.Length);
        }

        public async Task ReceivePackets()
        {
            _dataStream = TcpClientData!.GetStream();

            byte[] buffer = new byte[_packetSize];
            int bytesRead = await _dataStream!.ReadAsync(buffer, 0, buffer.Length);
            var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Packet? packet = JsonConvert.DeserializeObject<Packet>(stringMessage)!;

            if(packet == null)
            {
                return;
            }

            switch (packet.ContentType)
            {
                case MessageType.FrameChunk:
                    byte[] chunk = JsonConvert.DeserializeObject<byte[]>(packet.Payload!)!;
                    ChunkHandler(chunk);
                    break;

                case MessageType.FrameEnd:
                    byte[] bitmapBytes = frameChunks.ToArray();
                    frameChunks.Clear();
                    BitmapImage? frame;

                    using (MemoryStream mstream = new(bitmapBytes))
                    {
                        frame = new BitmapImage();
                        frame.BeginInit();
                        frame.StreamSource = mstream;
                        frame.CacheOption = BitmapCacheOption.OnLoad;
                        frame.EndInit();
                    }
                    FrameHandler(frame);
                    break;

                case MessageType.Message:
                    string message = JsonConvert.DeserializeObject<string>(packet.Payload!)!;
                    ChatHandler(message);
                    break;

                case MessageType.FileChunk:
                    if (!ReceivingFile)
                    {
                        ReceivingFileName = JsonConvert.DeserializeObject<string>(packet.Payload!)!;
                    }

                    byte[] fileChunk = JsonConvert.DeserializeObject<byte[]>(packet.Payload!)!;
                    FileChunkHandler(fileChunk);
                    break;

                case MessageType.FileEnd:
                    ReceivingFile = false;
                    byte[] fileBytes = fileChunks.ToArray();
                    fileChunks.Clear();

                    File.WriteAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\{ReceivingFileName}", fileBytes);

                    ChatHandler($"Received {ReceivingFileName} located at {AppDomain.CurrentDomain.BaseDirectory}\\{ReceivingFileName} from remote computer.");
                    break;
            }
        }
    }
}
