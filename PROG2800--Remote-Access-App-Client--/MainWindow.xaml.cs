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
        }

        ~MainWindow()
        {
            _connectionManager.TcpListener!.Stop();
            _connectionManager.TcpClient!.Dispose();
        }

        private void ServerStatusUpdate(string message)
        {
            if (message == "Online.")
            {
                lblAppStatus.Foreground = Brushes.Green;
            }
            else
            {
                lblAppStatus.Foreground = Brushes.Red;
            }

            lblAppStatus.Content = message;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LocalMessageEvent("Stopping server...");
                _connectionManager.TcpListener!.Stop();

                if (_connectionManager.TcpClient != null)
                    _connectionManager.TcpClient!.Close();

                ServerStatusUpdate("Offline.");
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
        }

        private void btnLogs_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LocalMessageEvent("Starting server...");
                _connectionManager.TcpListener = new(IPAddress.Any, _connectionManager.Port);
                _connectionManager.TcpListener.Start();
                LocalMessageEvent("Server started!");
                ServerStatusUpdate("Online.");

                LocalMessageEvent($"Listening on port {_connectionManager.Port}.");

                btnStartServer.Click -= btnStartServer_Click;
                btnStartServer.Click += Stop_Click;
                btnStartServer.Content = "Stop the Server";

                btnRequestConnection.IsEnabled = false;
                txtServerIp.IsEnabled = false;
                txtPort.IsEnabled = false;

                //ChatGPT get external IP.
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.GetAsync("http://api.ipify.org");
                        response.EnsureSuccessStatusCode();
                        txtServerIp.Text = await response.Content.ReadAsStringAsync();
                    }
                }
                catch (Exception ex)
                {
                    txtServerIp.Text = "ERROR";
                }
                

                while (!_connectionManager.IsConnected)
                {
                    _connectionManager.TcpClient = await _connectionManager.TcpListener!.AcceptTcpClientAsync();
                    LocalMessageEvent($"Connection Established with {_connectionManager.TcpClient.Client.RemoteEndPoint} on port {_connectionManager.Port}.");
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