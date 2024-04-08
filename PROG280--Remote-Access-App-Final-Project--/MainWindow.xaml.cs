using PROG280__Remote_Access_App_Data__;
using PROG280__Remote_Access_App_Server__;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

namespace PROG280__Remote_Access_App_Server__
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly ConnectionManager _connectionManager = new();

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
                lblServerStatus.Foreground = Brushes.Green;
            }
            else
            {
                lblServerStatus.Foreground = Brushes.Red;
            }

            lblServerStatus.Content = message;
            OnPropertyChanged();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LocalMessageEvent("Starting server...");
                _connectionManager.TcpListener = new(IPAddress.Any, _connectionManager.Port);
                _connectionManager.TcpListener.Start();
                LocalMessageEvent("Server started!");
                ServerStatusUpdate("Online.");

                LocalMessageEvent($"Listening on port {_connectionManager.Port}.");

                btnStart.Click -= Start_Click;
                btnStart.Click += Stop_Click;
                btnStart.Content = "Stop";

                while (!_connectionManager.IsConnected)
                {
                    _connectionManager.TcpClient = await _connectionManager.TcpListener!.AcceptTcpClientAsync();
                    LocalMessageEvent($"Connection Established with {_connectionManager.TcpClient.Client.RemoteEndPoint} on port {_connectionManager.Port}.");
                }
            }
            catch (Exception ex)
            {
                LocalMessageEvent($"Listening Connection failed with error: {ex.Message}");
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LocalMessageEvent("Stopping server...");
                _connectionManager.TcpListener!.Stop();

                if(_connectionManager.TcpClient != null)
                    _connectionManager.TcpClient!.Close();

                ServerStatusUpdate("Offline.");
                LocalMessageEvent("Server stopped.");
                btnStart.Click -= Stop_Click;
                btnStart.Click += Start_Click;
                btnStart.Content = "Start";
            }
            catch (Exception ex)
            {
                LocalMessageEvent($"Something went wrong, error: {ex.Message}");
            }
        }
    }
}