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
        private ConnectionManager _connectionManager;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
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


        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            _connectionManager.LocalMessageHandler("Starting server...");
            _connectionManager.TcpListener = new(IPAddress.Any, _connectionManager.Port);
            _connectionManager.TcpListener.Start();

            _connectionManager.LocalMessageHandler($"Listening on port {_connectionManager.Port}.");

            while (!_clientConnected)
            {
                try
                {
                    tcpClient = await tcpListener.AcceptTcpClientAsync();
                    _clientConnected = true;
                    LocalServerMessage($"Connection Established with {tcpClient.Client.RemoteEndPoint} on port {Port}.");
                }
                catch (Exception ex) 
                {
                    LocalServerMessage($"Connection failed with error: {ex.Message}");
                    break;
                }
            }
        }
    }
}