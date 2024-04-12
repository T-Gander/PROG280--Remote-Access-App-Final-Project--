using Newtonsoft.Json;
using PROG280__Remote_Access_App_Client__;
using PROG280__Remote_Access_App_Data__;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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

namespace PROG2800__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for RemoteWindow.xaml
    /// </summary>
    public partial class RemoteWindow : Window
    {
        private RemoteWindowDataContext _RemoteWindowDataContext = new();

        public RemoteWindow()
        {
            InitializeComponent();
            //OnReceivePackets += RemoteWindow_OnReceivePackets;
            DataContext = _RemoteWindowDataContext;
            Task.Run(HandlePackets);
        }

        private async void HandlePackets()
        {
            try
            {
                while(true)
                {
                    await Dispatcher.Invoke(async () =>
                    {
                        _RemoteWindowDataContext.Frame = await ServerWindow.ClientConnection!.ReceiveVideoPackets();
                    });
                }
            }
            catch (Exception ex)
            {
                //Something bad happened
            }
        }

    }
}
