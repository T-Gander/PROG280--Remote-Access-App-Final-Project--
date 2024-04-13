﻿using System.Net.Sockets;
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

        private MessagingWindow? _messagingWindow;

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
        public int VideoPort
        {
            get
            {
                return int.Parse(_videoPort);
            }
            set
            {
                if (int.TryParse(value.ToString(), out int result))
                {
                    _videoPort = result.ToString();
                }
                else
                {
                    _videoPort = "9000";
                }
                OnPropertyChanged(nameof(VideoPort));
            }
        }

        private string _videoPort = "9000";

        public int MessagePort
        {
            get
            {
                return int.Parse(_messagePort);
            }
            set
            {
                if (int.TryParse(value.ToString(), out int result))
                {
                    _messagePort = result.ToString();
                }
                else
                {
                    _messagePort = "9000";
                }
                OnPropertyChanged(nameof(VideoPort));
            }
        }

        private string _messagePort = "9001";

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
            LocalMessageEvent += NetworkConnected.AddToLogMessagesList;
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
            ServerConnection = new();

            btnRequestConnection.IsEnabled = false;
            txtServerIp.IsEnabled = false;

            LocalMessageEvent("Starting server...");
            ServerConnection.TcpVideoListener = new(IPAddress.Any, VideoPort);
            ServerConnection.TcpVideoListener.Start();
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

                await LocalMessageEvent($"Listening on video port {VideoPort} and message port {MessagePort}.");

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
            ServerConnection!.TcpVideoClient = await ServerConnection!.TcpVideoListener!.AcceptTcpClientAsync();
            ServerConnection!.TcpMessageClient = await ServerConnection!.TcpMessageListener!.AcceptTcpClientAsync();

            //await ServerConnection!.InitializeMessaging(ServerConnection.TcpVideoClient.Client.RemoteEndPoint!.ToString()!, MessagePort);
            await LocalMessageEvent($"Connection Established with {ServerConnection!.TcpVideoClient.Client.RemoteEndPoint}.");
            ServerConnection!.IsConnected = true;
            _messagingWindow = new MessagingWindow(ServerConnection);
            _messagingWindow.Show();
        }

        private async void btnRequestConnection_Click(object sender, RoutedEventArgs e)
        {
            ClientConnection = new();
            await LocalMessageEvent($"Attempting to connect to {RemoteIPAddress}");

            try
            {
                ClientConnection!.TcpMessageClient = await ClientConnection!.TcpMessageListener!.AcceptTcpClientAsync();
                ClientConnection!.TcpVideoClient = new TcpClient(RemoteIPAddress, VideoPort);

                await ClientConnection!.InitializeMessaging(RemoteIPAddress, MessagePort);
            }
            catch
            {
                await LocalMessageEvent($"Connection Refused.");
                return;
            }

            ClientConnection!.IsConnected = true;

            await LocalMessageEvent($"Connected to {RemoteIPAddress}");

            var tcs = new TaskCompletionSource<object?>();

            // Use a TaskCompletionSource to create a task that completes when the RemoteWindow is closed

            void RemoteWindow_Closed(object sender, EventArgs e)
            {
                tcs.TrySetResult(null); // Signal that the task is completed
            }

            RemoteWindow _remoteWindow = new(RemoteIPAddress);
            _remoteWindow.Closed += RemoteWindow_Closed;
            _remoteWindow.Show();

            _messagingWindow = new MessagingWindow(ClientConnection);
            _messagingWindow.Show();

            // Wait asynchronously for the RemoteWindow to be closed
            await tcs.Task;

            if (_messagingWindow != null)
            {
                _messagingWindow.Close();
            }

            ClientConnection.CloseConnections();
            await LocalMessageEvent("Connection closed.");
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog();
        }
    }
}