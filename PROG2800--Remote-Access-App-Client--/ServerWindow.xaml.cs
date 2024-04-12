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

namespace PROG280__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ServerWindow : INotifyPropertyChanged
    {
        public enum FrameRate { Thirty = 34, Sixty = 17, OneTwenty = 9 }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static Client? ClientConnection;
        public static Server? ServerConnection;
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

        public string RemoteIPAddress
        {
            get
            {
                return _remoteIPAddress;
            }
            set
            {
                _remoteIPAddress = value;
                OnPropertyChanged(nameof(RemoteIPAddress));
            }
        }

        private string _remoteIPAddress = "";

        public delegate Task LocalMessageDelegate(string message);
        public delegate void PacketDelegate(Packet packet);
        public event LocalMessageDelegate LocalMessageEvent;

        public ServerWindow()
        {
            InitializeComponent();
            DataContext = this;
            LocalMessageEvent += NetworkConnected.AddToMessagesList;
            LocalMessageEvent += ServerStatusUpdate;
            _logWindow = new();
        }

        private async Task ServerStatusUpdate(string message)
        {
            if (message == "Server started!")
            {
                lblAppStatus.Foreground = Brushes.Green;
            }
            else
            {
                lblAppStatus.Foreground = Brushes.Black;
            }

            lblAppStatus.Text = message;

            await Task.Delay(1000);
        }

        private async void StopServer()
        {
            ServerConnection!.ShutDown();
            await LocalMessageEvent?.Invoke("Stopping server...")!;
            ChangeServerState();
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            StopServer();
        }

        private void btnLogs_Click(object sender, RoutedEventArgs e)
        {
            _logWindow.Show();
        }

        private void StartServer()
        {
            ServerConnection = new();

            btnRequestConnection.IsEnabled = false;
            txtServerIp.IsEnabled = false;
            txtPort.IsEnabled = false;

            LocalMessageEvent("Starting server...");
            ServerConnection.TcpListener = new(IPAddress.Any, Port);
            ServerConnection.TcpListener.Start();
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
                        txtPort.IsEnabled = true;

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
                    txtPort.IsEnabled = false;
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

                while (true)
                {
                    if (!ServerConnection!.IsConnected)
                    {
                        await LocalMessageEvent("Lost Connection to Remote Client.");
                        await LocalMessageEvent("Listening...");
                        await Listen();
                    }
                    else
                    {
                        await ServerConnection!.SendVideoPackets();
                    }
                    await Task.Delay((int)FrameRate.Thirty); //Tied to fps
                }
            }
            catch (Exception ex)
            {
                await LocalMessageEvent($"TCP Listener Closed.");
            }
        }

        private async Task Listen()
        {
            await LocalMessageEvent("Listening...");
            ServerConnection!.TcpVideoClient = await ServerConnection!.TcpListener!.AcceptTcpClientAsync();
            await LocalMessageEvent($"Connection Established with {ServerConnection!.TcpVideoClient.Client.RemoteEndPoint}.");
            ServerConnection!.IsConnected = true;
        }

        private async void btnRequestConnection_Click(object sender, RoutedEventArgs e)
        {
            ClientConnection = new();
            await LocalMessageEvent($"Attempting to connect to {RemoteIPAddress}");

            try
            {
                ClientConnection!.TcpVideoClient = new TcpClient(RemoteIPAddress, Port);
            }
            catch
            {
                await LocalMessageEvent($"Connection Refused.");
                return;
            }

            ClientConnection!.IsConnected = true;

            await LocalMessageEvent($"Connected to {RemoteIPAddress}");

            RemoteWindow _remoteWindow = new(RemoteIPAddress);
            _remoteWindow.ShowDialog();
            ClientConnection.CloseConnections();
            await LocalMessageEvent("Connection closed.");
        }
    }
}