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
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using PROG280__Remote_Access_App_Client__;
using static PROG280__Remote_Access_App_Client__.RemoteWindow;


namespace PROG280__Remote_Access_App_Data__
{
    public class NetworkConnected : INotifyPropertyChanged
    {
        public RemoteWindow RemoteWindow { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public delegate void FrameDelegate(BitmapImage frame);
        public event FrameDelegate? FrameHandler;

        public delegate void ChunkDelegate(byte[] data);
        public event ChunkDelegate ChunkHandler;
        public event ChunkDelegate FileChunkHandler;

        public delegate void ChatDelegate(string message);
        public event ChatDelegate ChatHandler;

        public bool ReceivingFile = false;
        public string ReceivingFileName = "";

        public bool FrameReady = false;

        public NetworkConnected()
        {
            ChunkHandler += HandleFrameChunks;
            FileChunkHandler += HandleFileChunks;
            ChatHandler += HandleChatMessages;
            FrameHandler += HandleFrames;
        }

        private List<byte> frameChunks = new List<byte>();

        private List<byte> fileChunks = new List<byte>();

        public bool IsConnected { get; set; } = false;

        public int ChunkSize = 1024;
        public int PacketSize = 1500;

        public string? DisplayName { get; set; } = "Lazy User";

        public TcpListener? TcpListenerData { get; set; }
        public TcpListener? TcpListenerVideo { get; set; }

        public TcpClient? TcpClientData { get; set; }

        public TcpClient? TcpClientVideo { get; set; }

        protected NetworkStream? _dataStream { get; set; }

        protected NetworkStream? _videoStream { get; set; }

        public ObservableCollection<string> LogMessages { get; set; } = new();

        public ObservableCollection<string> ChatMessages {  get; set; } = new();

        private BitmapImage? _frame;

        public BitmapImage CurrentFrame
        {
            get
            {
                return _frame;
            }
            set
            {
                _frame = value;
                OnPropertyChanged(nameof(Frame));
            }
        }

        public Task AddToLogMessagesList(string message)
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

            byte[] ackBytes = new byte[PacketSize];

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

        private void HandleChatMessages(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Perform UI-related operations inside this block
                // For example, adding items to a collection bound to a UI control
                ChatMessages.Add($"{DisplayName}: {message}");
            });
        }

        private void HandleFrames(BitmapImage? frame)
        {
            if(frame != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Perform UI-related operations inside this block
                    // For example, adding items to a collection bound to a UI control
                    //CurrentFrame = frame;
                    CurrentFrame = frame;
                });
            }
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

        public async Task SendDataPacket<T>(MessageType messageType, T data)
        {
            _dataStream = TcpClientData!.GetStream();

            byte[] fileBytes = new byte[PacketSize];

            Packet filePacket = new Packet()
            {
                ContentType = messageType,
                Payload = JsonConvert.SerializeObject(data)
            };

            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(filePacket)).CopyTo(fileBytes, 0);

            await _dataStream.WriteAsync(fileBytes, 0, fileBytes.Length);
        }

        public async Task SendVideoPacket<T>(MessageType messageType, T data)
        {
            _videoStream = TcpClientVideo!.GetStream();

            byte[] fileBytes = new byte[PacketSize];

            Packet filePacket = new Packet()
            {
                ContentType = messageType,
                Payload = JsonConvert.SerializeObject(data)
            };

            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(filePacket)).CopyTo(fileBytes, 0);

            await _videoStream.WriteAsync(fileBytes, 0, fileBytes.Length);
        }

        public async Task ReceiveVideoPackets()
        {
            try
            {
                while (true)
                {
                    _videoStream = TcpClientVideo!.GetStream();

                    byte[] buffer = new byte[PacketSize];
                    int bytesRead = await _videoStream!.ReadAsync(buffer, 0, buffer.Length);
                    var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Packet? packet = JsonConvert.DeserializeObject<Packet>(stringMessage)!;

                    if (packet == null)
                    {
                        return;
                    }

                    switch (packet.ContentType)
                    {
                        case MessageType.FrameChunk:
                            while (FrameReady)
                            {
                                await Task.Delay(1000);
                            }

                            byte[] chunk = JsonConvert.DeserializeObject<byte[]>(packet.Payload!)!;
                            ChunkHandler(chunk);
                            break;

                        case MessageType.FrameEnd:
                            byte[] bitmapBytes = frameChunks.ToArray();
                            frameChunks.Clear();
                            BitmapImage? frame = new();

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
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task ReceiveDataPackets()
        {
            try
            {
                while (true)
                {
                    _dataStream = TcpClientData!.GetStream();

                    byte[] buffer = new byte[PacketSize];
                    int bytesRead = await _dataStream!.ReadAsync(buffer, 0, buffer.Length);
                    var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Packet? packet = JsonConvert.DeserializeObject<Packet>(stringMessage)!;

                    if (packet == null)
                    {
                        return;
                    }

                    switch (packet.ContentType)
                    {
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
            catch (Exception ex)
            {

            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
