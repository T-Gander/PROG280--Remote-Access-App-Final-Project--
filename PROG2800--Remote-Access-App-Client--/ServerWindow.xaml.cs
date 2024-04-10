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

namespace PROG280__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        public static ConnectionManager ConnectionManager = new();

        private LogsWindow _logWindow;

        private RemoteWindow? _remoteWindow;

        public delegate void LocalMessageDelegate(string message);
        public delegate void PacketDelegate(Packet packet);
        public event LocalMessageDelegate LocalMessageEvent;

        public ServerWindow()
        {
            InitializeComponent();
            DataContext = ConnectionManager;
            LocalMessageEvent += ConnectionManager!.AddToMessagesList;
            LocalMessageEvent += ServerStatusUpdate;
            ConnectionManager.LocalIPAddress = RetreiveLocalIP();
            _logWindow = new();
        }

        ~ServerWindow()
        {
            ConnectionManager.TcpListener!.Stop();
            ConnectionManager.TcpListener.Dispose();
            ConnectionManager.TcpVideoClient!.Dispose();
        }

        private void ServerStatusUpdate(string message)
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
        }

        private async Task StopServer()
        {
            LocalMessageEvent("Stopping server...");
            ChangeServerState();
            await Task.Delay(1000);
        }

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            await StopServer();
        }

        private void btnLogs_Click(object sender, RoutedEventArgs e)
        {
            _logWindow.Show();
        }

        private void StartServer()
        {
            btnRequestConnection.IsEnabled = false;
            txtServerIp.IsEnabled = false;
            txtPort.IsEnabled = false;

            LocalMessageEvent("Starting server...");
            Task.Delay(1000);
            ConnectionManager.TcpListener = new(IPAddress.Any, ConnectionManager.Port);
            ConnectionManager.TcpListener.Start();
            LocalMessageEvent("Server started!");
        }

        private async void ChangeServerState()
        {
            switch (btnStartServer.Content)
            {
                case "Stop the Server":
                    try
                    {
                        if (ConnectionManager.TcpVideoClient != null)
                            ConnectionManager.TcpVideoClient!.Close();

                        ConnectionManager.TcpListener!.Stop();

                        await Task.Delay(1000);
                        LocalMessageEvent("Server stopped.");

                        ConnectionManager.IsConnected = false;

                        btnStartServer.Click -= Stop_Click;
                        btnStartServer.Click += btnStartServer_Click;
                        btnStartServer.Content = "Start a Server";

                        btnRequestConnection.IsEnabled = true;
                        txtServerIp.IsEnabled = true;
                        txtPort.IsEnabled = true;

                        await Task.Delay(1000);
                        LocalMessageEvent("Waiting for Action...");
                    }
                    catch (Exception ex)
                    {
                        LocalMessageEvent($"Something went wrong, error: {ex.Message}");
                    }
                    break;

                case "Start a Server":
                    ConnectionManager.IsServer = true;

                    btnStartServer.Click -= btnStartServer_Click;
                    btnStartServer.Click += Stop_Click;
                    btnStartServer.Content = "Stop the Server";

                    btnRequestConnection.IsEnabled = false;
                    txtServerIp.IsEnabled = false;
                    txtPort.IsEnabled = false;
                    break;
            }
        }

        private bool TryRetreiveIP()
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
                        LocalMessageEvent($"Found IP: {ipstring}");
                }

                txtServerIp.Text = $"{localIPs[0]}";

                LocalMessageEvent($"Selected IP: {txtServerIp.Text}");
                return true;
            }
            catch (Exception ex)
            {
                txtServerIp.Text = "ERROR";
                LocalMessageEvent($"{ex.Message}");
                return false;
            }
        }

        private string RetreiveLocalIP()
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
                LocalMessageEvent($"{ex.Message}");
                return "Error, Check Logs.";
            }
        }

        private async void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeServerState();

                StartServer();
                await Task.Delay(1000);

                LocalMessageEvent($"Listening on port {ConnectionManager.Port}.");
                await Task.Delay(1000);

                LocalMessageEvent("Retreiving external IP...");
                await Task.Delay(1000);

                if (!TryRetreiveIP())
                {
                    await StopServer();
                    return;
                }
                await Task.Delay(1000);

                await CheckConnection();
            }
            catch (Exception ex)
            {
                LocalMessageEvent($"Error logged: {ex.Message})");
                await Task.Delay(1000); //This will run at the same time as clicking stop
                LocalMessageEvent($"ERROR, Check Logs.");
            }
        }

        private async Task CheckConnection()
        {
            try
            {
                await Listen();

                while (true)
                {
                    if (!ConnectionManager.IsConnected)
                    {
                        await Listen();
                    }
                    else
                    {
                        await ConnectionManager.SendVideoPackets();
                    }
                    await Task.Delay(1000); //Tied to fps?
                }
            }
            catch (Exception ex)
            {
                LocalMessageEvent($"TCP Listener Closed.");
                await Task.Delay(1000);
            }
        }

        private async Task Listen()
        {
            LocalMessageEvent("Listening...");
            ConnectionManager.TcpVideoClient = await ConnectionManager.TcpListener!.AcceptTcpClientAsync();
            LocalMessageEvent($"Connection Established with {ConnectionManager.TcpVideoClient.Client.RemoteEndPoint}.");
            ConnectionManager.IsConnected = true;

            await Task.Delay(1000);
        }

        private async void btnRequestConnection_Click(object sender, RoutedEventArgs e)
        {
            ConnectionManager.IsServer = false;
            LocalMessageEvent($"Attempting to connect to {ConnectionManager.RemoteIPAddress}");
            await Task.Delay(1000);

            ConnectionManager.TcpVideoClient = new TcpClient(ConnectionManager.RemoteIPAddress.ToString(), ConnectionManager.Port);

            ConnectionManager.IsConnected = true;

            LocalMessageEvent($"Connected to {ConnectionManager.RemoteIPAddress}");

            _remoteWindow = new();
            _remoteWindow.ShowDialog();
            LocalMessageEvent("Connection closed.");
        }
    }
}