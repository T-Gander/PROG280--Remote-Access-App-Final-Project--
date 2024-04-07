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
        public delegate void LocalServerMessageDelegate(string message);
        private event LocalServerMessageDelegate LocalServerMessage;

        private bool _isOnline = false;
        private bool _clientConnected = false;
        private int _port;

        public int Port {
            get { return _port; }
            set 
            {
                if(int.TryParse(value.ToString(), out int parsedValue))
                {
                    _port = parsedValue;
                    LocalServerMessage($"Port set to {_port}.");
                }
                else
                {
                    ServerStatusUpdate("Invalid Port Detected.");
                };
            }
        }

        public bool IsOnline
        {
            get
            {
                return _isOnline;
            }
            set
            {
                if(_isOnline != value )
                {
                    _isOnline = value;
                    switch (_isOnline)
                    {
                        case false:
                            ServerStatusUpdate("Offline.");
                            break;

                        default:
                            ServerStatusUpdate("Online.");
                            break;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LocalServerMessage += SendToMessageList;
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


        public void SendToMessageList(string message)
        {
            lbServerMessages.Items.Add(message);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            TcpListener tcpListener = new(IPAddress.Any, Port);
            LocalServerMessage($"Listening on port {Port}.");
            tcpListener.Start();

            while(!_clientConnected)
            {
                try
                {
                    using TcpClient handler = await tcpListener.AcceptTcpClientAsync();
                    LocalServerMessage($"Connection Established with {handler.Client.RemoteEndPoint} on port {Port}.");
                }
                catch (Exception ex) 
                {
                    LocalServerMessage($"Connection failed with error: {ex.Message}");
                    break;
                }
            }

            if (_clientConnected)
            {
                IsOnline = true;
            }
            else
            {
                IsOnline = false;
            }
            
        }
    }
}