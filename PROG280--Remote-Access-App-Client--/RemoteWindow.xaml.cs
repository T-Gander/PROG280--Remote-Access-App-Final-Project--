using Newtonsoft.Json;
using PROG280__Remote_Access_App_Data__;
using PROG2800__Remote_Access_App_Client__.MessagingWindowComponents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PROG280__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for RemoteWindow.xaml
    /// </summary>
    public partial class RemoteWindow : Window
    {
        private NetworkConnected _Client;

        private RemoteWindowDataContext _RemoteWindowDataContext = new();

        public RemoteWindow(NetworkConnected client)
        {
            InitializeComponent();
            DataContext = _RemoteWindowDataContext;
            _Client = client;
            Task.Run(HandlePackets);
        }

        private async void HandlePackets()
        {
            try
            {
                while (true)
                {
                    await Dispatcher.Invoke(async () =>
                    {
                        _RemoteWindowDataContext.Frame = await _Client.ReceiveVideoPackets();
                    });
                }
            }
            catch (Exception ex)
            {
                //Something bad happened
            }
        }

        private void frame_Click(object sender, RoutedEventArgs e)
        {
            DisplayName _displayNameWindow = new();
            _displayNameWindow.ShowDialog();
        }
    }
}
