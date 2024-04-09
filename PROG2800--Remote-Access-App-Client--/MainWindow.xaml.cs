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

namespace PROG280__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ConnectionManager ConnectionManager = new();

        private LogsWindow _logWindow;

        //public async void Connect(long ipAddress)
        //{
        //    var ipEndPoint = new IPEndPoint(ipAddress, 13);

        //    using TcpClient client = new();
        //    await client.ConnectAsync(ipEndPoint);
        //    await using NetworkStream stream = client.GetStream();

        //    var buffer = new byte[1_024];
        //    await stream.WriteAsync(buffer);
        //}

        public delegate void LocalMessageDelegate(string message);
        public event LocalMessageDelegate LocalMessageEvent;

        private bool _attemptingConnection = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = ConnectionManager;
            LocalMessageEvent += ConnectionManager!.AddToMessagesList;
            LocalMessageEvent += ServerStatusUpdate;
            _logWindow = new(ConnectionManager);
        }

        ~MainWindow()
        {
            ConnectionManager.TcpListener!.Stop();
            ConnectionManager.TcpListener.Dispose();
            ConnectionManager.TcpClient!.Dispose();
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
            _attemptingConnection = false;
        }

        private void btnLogs_Click(object sender, RoutedEventArgs e)
        {
            _logWindow.Show();
        }

        private void StartServer()
        {
            LocalMessageEvent("Starting server...");
            ConnectionManager.TcpListener = new(IPAddress.Any, ConnectionManager.Port);
            ConnectionManager.TcpListener.Start();
            LocalMessageEvent("Server started!");
        }

        private async void ChangeServerButtons()
        {
            switch (btnStartServer.Content)
            {
                case "Stop the Server":
                    try
                    {
                        ConnectionManager.TcpListener!.Stop();

                        var listenerClose = Task.Run(async () =>
                        {
                            while (_attemptingConnection == true)
                            {
                                await Task.Delay(1000);
                            }
                            return;
                        });

                        await Task.WhenAll(listenerClose);

                        if (ConnectionManager.TcpClient != null)
                            ConnectionManager.TcpClient!.Close();

                        await Task.Delay(2000);
                        LocalMessageEvent("Server stopped.");
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

                LocalMessageEvent($"Listening on port {ConnectionManager.Port}.");
                await Task.Delay(1000);

                ChangeServerButtons();

                LocalMessageEvent("Retreiving external IP...");
                await Task.Delay(1000);

                if (!TryRetreiveIP())
                {
                    return;
                }
                await Task.Delay(1000);

                LocalMessageEvent("Listening...");
                ConnectionManager.TcpClient = await ConnectionManager.TcpListener!.AcceptTcpClientAsync();
                LocalMessageEvent($"Connection Established with {ConnectionManager.TcpClient.Client.RemoteEndPoint}.");
            }
            catch (Exception ex)
            {
                await Task.Delay(1000); //This will run at the same time as clicking stop
                LocalMessageEvent($"TCP Listener Closed.");
            }
        }

        private void btnRequestConnection_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}