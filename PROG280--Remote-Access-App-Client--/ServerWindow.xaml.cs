using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PROG280__Remote_Access_App_Data__;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Xml.Serialization;
using PROG280__Remote_Access_App_Client__;
using Newtonsoft.Json;
using PROG2800__Remote_Access_App_Client__;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using static PROG280__Remote_Access_App_Data__.Packet;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;

namespace PROG280__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ServerWindow : INotifyPropertyChanged
    {
        public enum FrameRate { Thirty = 34, Sixty = 17, OneTwenty = 9 }

        private MessagingWindow? _messagingWindow;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NetworkConnected? Client = new();
        private LogsWindow _logWindow;

        public string LocalIPAddress
        {
            get
            {
                return RetreiveLocalIP().Result;
            }
        }
        public int Port
        {
            get
            {
                return int.Parse(_port);
            }
            set
            {
                if (int.TryParse(value.ToString(), out int result))
                {
                    _port = result.ToString();
                }
                else
                {
                    _port = "9000";
                }
                OnPropertyChanged(nameof(Port));
            }
        }

        private string _port = "9000";

        public string RemoteIPAddress { get; set; }

        public delegate Task LocalMessageDelegate(string message);
        public event LocalMessageDelegate LocalMessageEvent;

        public ServerWindow()
        {
            InitializeComponent();
            DataContext = this;
            LocalMessageEvent += Client.AddToLogMessagesList;
            LocalMessageEvent += ServerStatusUpdate;
            _logWindow = new(Client);
        }

        private async Task ServerStatusUpdate(string message)
        {
            lblAppStatus.Text = message;

            await Task.Delay(1000);
        }

        private async void StopServer()
        {
            //Client!.ShutDown();
            await LocalMessageEvent?.Invoke("Stopping server...")!;
            ChangeServerState();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            StopServer();
        }

        private void btnLogs_Click(object sender, RoutedEventArgs e)
        {
            _logWindow.Show();
        }

        private void StartServer()
        {
            Client = new();

            btnRequestConnection.IsEnabled = false;
            txtServerIp.IsEnabled = false;

            LocalMessageEvent("Starting server...");
            Client!.TcpListenerData = new(IPAddress.Any, Port);
            Client.TcpListenerData.Start();
            
            LocalMessageEvent("Server started!");
        }

        private async void ChangeServerState()
        {
            switch (btnStartServer.Content)
            {
                case "Stop the Server":
                    try
                    {
                        await LocalMessageEvent("Server stopped.");

                        btnStartServer.Click -= Stop_Click;
                        btnStartServer.Click += btnStartServer_Click;
                        btnStartServer.Content = "Start a Server";

                        btnRequestConnection.IsEnabled = true;
                        txtServerIp.IsEnabled = true;

                        await LocalMessageEvent("Waiting for Action...");
                    }
                    catch (Exception ex)
                    {
                        await LocalMessageEvent($"Something went wrong, error: {ex.Message}");
                    }
                    break;

                case "Start a Server":
                    btnStartServer.Click -= btnStartServer_Click;
                    btnStartServer.Click += Stop_Click;
                    btnStartServer.Content = "Stop the Server";

                    btnRequestConnection.IsEnabled = false;
                    txtServerIp.IsEnabled = false;
                    break;
            }
        }

        private async Task<bool> TryRetreiveIP()
        {
            try
            {
                string hostName = Dns.GetHostName();
                List<IPAddress> localIPs = Dns.GetHostAddresses(hostName).ToList();

                for (int i = 0; i < localIPs.Count - 1; i++)
                {
                    string ipstring = localIPs[i].ToString();
                    if (ipstring.Contains(":"))
                    {
                        localIPs.RemoveAt(i);
                        i--;
                    }
                    else
                        await LocalMessageEvent($"Found IP: {ipstring}");
                }

                txtServerIp.Text = $"{localIPs[0]}";

                await LocalMessageEvent($"Selected IP: {txtServerIp.Text}");
                return true;
            }
            catch (Exception ex)
            {
                txtServerIp.Text = "ERROR";
                await LocalMessageEvent($"{ex.Message}");
                return false;
            }
        }

        private async Task<string> RetreiveLocalIP()
        {
            try
            {
                string hostName = Dns.GetHostName();
                List<IPAddress> localIPs = Dns.GetHostAddresses(hostName).ToList();

                for (int i = 0; i < localIPs.Count - 1; i++)
                {
                    string ipstring = localIPs[i].ToString();
                    if (ipstring.Contains(":"))
                    {
                        localIPs.RemoveAt(i);
                        i--;
                    }
                }

                return localIPs[0].ToString();
            }
            catch (Exception ex)
            {
                await LocalMessageEvent($"{ex.Message}");
                return "Error, Check Logs.";
            }
        }

        private async void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeServerState();

                StartServer();

                await LocalMessageEvent($"Listening on port {Port}.");

                await LocalMessageEvent("Retreiving external IP...");

                if (!await TryRetreiveIP())
                {
                    StopServer();
                    return;
                }

                await CheckConnection();
            }
            catch (Exception ex)
            {
                await LocalMessageEvent($"Error logged: {ex.Message})");
                await LocalMessageEvent($"ERROR, Check Logs.");
            }
        }

        private async Task CheckConnection()
        {
            try
            {
                await Listen();

                int i = 0;

                while (i < 1)
                {
                    if (!Client!.IsConnected)
                    {
                        await LocalMessageEvent("Lost Connection to Remote Client.");
                        await LocalMessageEvent("Listening...");
                        await Listen();
                    }
                    else
                    {
                        byte[] imageData;
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            Client!.GrabScreen().Save(memoryStream, ImageFormat.Png); // Save the bitmap to the memory stream as PNG format
                            imageData = memoryStream.ToArray(); // Get the byte array from the memory stream
                        }

                        List<byte> frameChunks = new List<byte>();

                        int totalChunks = (int)Math.Ceiling((double)imageData.Length / (double)1024);
                        int chunkIndex = 0;

                        while (chunkIndex != totalChunks)
                        {
                            int offset = chunkIndex * Client!.ChunkSize;
                            int length = Math.Min(Client!.ChunkSize, imageData.Length - offset);
                            byte[] chunk = new byte[Client!.ChunkSize];
                            Buffer.BlockCopy(imageData, offset, chunk, 0, length);

                            // Serialize packet and send
                            await Client!.SendPacket(MessageType.FrameChunk, chunk);
                            chunkIndex++;
                        }

                        await Client!.SendPacket(MessageType.FrameEnd, new byte[Client.ChunkSize]);
                        i++;
                    }
                    await Task.Delay(1000); //Tied to fps
                }
            }
            catch (Exception ex)
            {
                await LocalMessageEvent(ex.Message);
                await LocalMessageEvent($"TCP Listener Closed.");
            }
        }

        private async Task Listen()
        {
            await LocalMessageEvent("Listening...");
            Client!.TcpClientData = await Client!.TcpListenerData!.AcceptTcpClientAsync();

            await LocalMessageEvent($"Connection Established with {Client!.TcpClientData.Client.RemoteEndPoint}.");
            Client!.IsConnected = true;
            _messagingWindow = new MessagingWindow(Client);
            _messagingWindow.Show();
        }

        private async void btnRequestConnection_Click(object sender, RoutedEventArgs e)
        {
            Client = new();
            await LocalMessageEvent($"Attempting to connect to {RemoteIPAddress}");

            try
            {
                Client!.TcpClientData = new TcpClient(RemoteIPAddress, Port);
            }
            catch
            {
                await LocalMessageEvent($"Connection Refused.");
                return;
            }

            Client!.IsConnected = true;

            await LocalMessageEvent($"Connected to {RemoteIPAddress}");

            var tcs = new TaskCompletionSource<object?>();

            // Use a TaskCompletionSource to create a task that completes when the RemoteWindow is closed

            void RemoteWindow_Closed(object sender, EventArgs e)
            {
                tcs.TrySetResult(null); // Signal that the task is completed
            }

            Client.RemoteWindow = new RemoteWindow(Client);

            Client.RemoteWindow.Closed += RemoteWindow_Closed;
            Client.RemoteWindow.Show();

            _messagingWindow = new MessagingWindow(Client);
            _messagingWindow.Show();

            // Wait asynchronously for the RemoteWindow to be closed
            await tcs.Task;

            if (_messagingWindow != null)
            {
                _messagingWindow.Close();
            }

            Client.CloseConnections();
            await LocalMessageEvent("Connection closed.");
        }

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            RemoteWindow remoteWindow = new RemoteWindow(Client);
            remoteWindow.Show();
            
        }

         //Tied to fps
    }
}