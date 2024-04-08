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

namespace PROG280__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ConnectionManager _connectionManager = new();

        //public async void Connect(long ipAddress)
        //{
        //    var ipEndPoint = new IPEndPoint(ipAddress, 13);

        //    using TcpClient client = new();
        //    await client.ConnectAsync(ipEndPoint);
        //    await using NetworkStream stream = client.GetStream();

        //    var buffer = new byte[1_024];
        //    await stream.WriteAsync(buffer);
        //}

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public delegate void LocalMessageDelegate(string message);
        public event LocalMessageDelegate LocalMessageEvent;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _connectionManager;
            LocalMessageEvent += _connectionManager!.AddToMessagesList;
            LocalMessageEvent += ServerStatusUpdate;
        }

        ~MainWindow()
        {
            _connectionManager.TcpListener!.Stop();
            _connectionManager.TcpListener.Dispose();
            _connectionManager.TcpClient!.Dispose();
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

        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            LocalMessageEvent("Stopping server...");
            ChangeServerButtons();
            await Task.Delay(1000);
        }

        private void btnLogs_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StartServer()
        {
            LocalMessageEvent("Starting server...");
            _connectionManager.TcpListener = new(IPAddress.Any, _connectionManager.Port);
            _connectionManager.TcpListener.Start();
            LocalMessageEvent("Server started!");
        }

        private void ChangeServerButtons()
        {
            switch (btnStartServer.Content)
            {
                case "Stop the Server":
                    try
                    {
                        _connectionManager.TcpListener!.Stop();

                        if (_connectionManager.TcpClient != null)
                            _connectionManager.TcpClient!.Close();

                        LocalMessageEvent("Server stopped.");
                        btnStartServer.Click -= Stop_Click;
                        btnStartServer.Click += btnStartServer_Click;
                        btnStartServer.Content = "Start a Server";
                        btnRequestConnection.IsEnabled = true;
                        txtServerIp.IsEnabled = true;
                        txtPort.IsEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        LocalMessageEvent($"Something went wrong, error: {ex.Message}");
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

        private async void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StartServer();
                await Task.Delay(1000);

                LocalMessageEvent($"Listening on port {_connectionManager.Port}.");
                await Task.Delay(1000);

                ChangeServerButtons();

                LocalMessageEvent("Retreiving external IP...");
                await Task.Delay(1000);

                if (!TryRetreiveIP())
                {
                    return;
                }
                await Task.Delay(1000);

                while (!_connectionManager.IsConnected)
                {
                    LocalMessageEvent("Listening...");
                    _connectionManager.TcpClient = await _connectionManager.TcpListener!.AcceptTcpClientAsync();
                    LocalMessageEvent($"Connection Established with {_connectionManager.TcpClient.Client.RemoteEndPoint}.");
                }
            }
            catch (Exception ex)
            {
                LocalMessageEvent($"Server failed with error: {ex.Message}");
            }
        }

        private void btnRequestConnection_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}