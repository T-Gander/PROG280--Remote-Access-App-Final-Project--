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
using SharpHook;
using System.Windows.Input;
using PROG280__Remote_Access_App_Client__.Data;


namespace PROG280__Remote_Access_App_Data__
{
    public class NetworkConnected : INotifyPropertyChanged
    {
        public RemoteWindow RemoteWindow { get; set; }
        public MessagingWindow MessagingWindow { get; set; }

        private double x = SystemParameters.PrimaryScreenWidth;
        private double y = SystemParameters.PrimaryScreenHeight;

        public EventSimulator MouseSimulator = new EventSimulator();

        public event PropertyChangedEventHandler? PropertyChanged;

        public delegate void FrameDelegate(BitmapImage frame);
        public event FrameDelegate? FrameHandler;

        public delegate void ChunkDelegate(byte[] data);
        public event ChunkDelegate ChunkHandler;
        public event ChunkDelegate FileChunkHandler;

        public delegate void ChatDelegate(string message);
        public event ChatDelegate ChatHandler;
        public event ChatDelegate LocalChatHandler;

        public delegate void RemoteControlDelegate(System.Windows.Point mouseEvent, SharpHook.Native.MouseButton button);
        public event RemoteControlDelegate? RemoteControlHandler;

        public bool AcceptReceivingFile = false;

        public event EventHandler SendingFileChanged;
        public event EventHandler AllowRemoteControlChanged;

        private bool _sendingFile;
        public bool SendingFile
        {
            get { return _sendingFile; }
            set
            {
                _sendingFile = value;
                OnSendingFileChanged();
            }
        }

        private bool _allowRemoteControl;
        public bool AllowRemoteControl
        {
            get { return _allowRemoteControl; }
            set
            {
                _allowRemoteControl = value;
                OnAllowRemoteControlChanged();
            }
        }

        protected virtual void OnSendingFileChanged()
        {
            SendingFileChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnAllowRemoteControlChanged()
        {
            AllowRemoteControlChanged?.Invoke(this, EventArgs.Empty);
        }

        public string ReceivingFileName = "";

        public bool FrameReady = false;

        public NetworkConnected()
        {
            ChunkHandler += HandleFrameChunks;
            FileChunkHandler += HandleFileChunks;
            ChatHandler += HandleChatMessages;
            LocalChatHandler += HandleLocalChatMessages;
            FrameHandler += HandleFrames;
            RemoteControlHandler += HandleRemoteControl;
        }

        private List<byte> frameChunks = new List<byte>();

        private List<byte> fileChunks = new List<byte>();

        public bool IsConnected { get; set; } = false;

        public int ChunkSize = 1024;
        public int PacketSize = 1500;

        public string? ChatName { get; set; } = "Lazy User";

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
                if(_frame == null)
                {
                    return new();
                }
                return _frame;
            }
            set
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _frame = value;
                    OnPropertyChanged(nameof(CurrentFrame));
                });
            }
        }

        public Task AddToLogMessagesList(string message)
        {
            LogMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task CloseConnections()
        {
            TcpListenerData?.Dispose();
            TcpListenerVideo?.Dispose();
            TcpClientVideo?.Dispose();
            TcpClientData?.Dispose();

            IsConnected = false;

            return Task.CompletedTask;
        }

        public static async Task<bool> ShowAcceptPopupAsync(string message, string caption)
        {
            // Show a dialog box asking the user whether to accept the file transfer
            MessageBoxResult result = await Task.Run(() =>
                MessageBox.Show(message, caption, MessageBoxButton.YesNo));

            // Return true if the user clicked Yes, false otherwise
            return result == MessageBoxResult.Yes;
        }

        private void HandleFileChunks(byte[] chunk)
        {
            fileChunks.AddRange(chunk);
        }

        private void HandleRemoteControl(System.Windows.Point mouseEvent, SharpHook.Native.MouseButton button)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MouseSimulator.SimulateMousePress((short)mouseEvent.X, (short)mouseEvent.Y, button);
                MouseSimulator.SimulateMouseRelease((short)mouseEvent.X, (short)mouseEvent.Y, button);
            });
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
                ChatMessages.Add($"{message}");
            });
        }

        private void HandleLocalChatMessages(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Perform UI-related operations inside this block
                // For example, adding items to a collection bound to a UI control
                ChatMessages.Add($"{message}");
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

        public async Task<BitmapImage?> ReceiveVideoPackets()
        {
            try
            {
                while (true)
                {
                    _videoStream = TcpClientVideo!.GetStream();
                    byte[] buffer = new byte[PacketSize];
                    int bytesRead = 0;
                    bytesRead = await _videoStream!.ReadAsync(buffer, 0, buffer.Length);
                    
                    var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Packet? packet = JsonConvert.DeserializeObject<Packet>(stringMessage)!;

                    if(packet != null)
                    {
                        switch (packet.ContentType)
                        {
                            case MessageType.FrameChunk:
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
                                return frame;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                return null;
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
                        break;
                    }

                    switch (packet.ContentType)
                    {
                        case MessageType.FileAccept:
                            SendingFile = true;
                            break;

                        case MessageType.FileDeny:
                            SendingFile = false;
                            break;


                        case MessageType.Message:
                            string message = JsonConvert.DeserializeObject<string>(packet.Payload!)!;
                            ChatHandler(message);
                            break;

                        case MessageType.FileChunk:
                            if (!AcceptReceivingFile)
                            {
                                ReceivingFileName = JsonConvert.DeserializeObject<string>(packet.Payload!)!;
                                LocalChatHandler($"The remote user is attempting to send a file... {ReceivingFileName}");

                                bool acceptFile = await ShowAcceptPopupAsync("Do you want to accept the file transfer?", "File Transfer");

                                if (!acceptFile)
                                {
                                    await SendDataPacket(MessageType.FileDeny, "N/A");
                                    break;
                                }
                                else
                                {
                                    //Send File Accept
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        MessagingWindow.DisableSendFiles();
                                    });

                                    await SendDataPacket(MessageType.FileAccept, "N/A");
                                    AcceptReceivingFile = true;
                                    break;
                                }
                            }

                            byte[] fileChunk = JsonConvert.DeserializeObject<byte[]>(packet.Payload!)!;
                            FileChunkHandler(fileChunk);
                            break;

                        case MessageType.FileEnd:
                            byte[] fileBytes = fileChunks.ToArray();
                            fileChunks.Clear();
                            AcceptReceivingFile = false;

                            File.WriteAllBytes($"{AppDomain.CurrentDomain.BaseDirectory}\\{ReceivingFileName}", fileBytes);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessagingWindow.EnableSendFiles();
                            });

                            LocalChatHandler($"Received {ReceivingFileName} located at {AppDomain.CurrentDomain.BaseDirectory}\\{ReceivingFileName}.");

                            break;

                        case MessageType.MouseMove:
                            //Popup to request control, set bool to continue to allow control, and then if a button is pressed control is released.
                            if (!AllowRemoteControl)
                            {
                                AllowRemoteControl = await ShowAcceptPopupAsync("Do you want to accept remote control? \n \n THE ONLY WAY TO REVOKE CONTROL IS TO USE CTRL+ALT+DELETE AND LOG OUT.", "Remote Control");
                            } 
                            else
                            {
                                MouseData mouseData = JsonConvert.DeserializeObject<MouseData>(packet.Payload!)!;

                                var a = x * mouseData.MouseLocation.X;
                                var b = y * mouseData.MouseLocation.Y;

                                System.Windows.Point convertedLocation = new System.Windows.Point(a, b);

                                RemoteControlHandler(convertedLocation, mouseData.MouseButton);
                            }

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
    }
}
