using Newtonsoft.Json;
using PROG280__Remote_Access_App_Client__;
using PROG280__Remote_Access_App_Data__;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private delegate void ReceivePackets(Packet packets);
        private event ReceivePackets OnReceivePackets;

        private RemoteWindowDataContext _RemoteWindowDataContext = new();

        public RemoteWindow()
        {
            InitializeComponent();
            OnReceivePackets += RemoteWindow_OnReceivePackets;
            Task.Run(HandlePackets);
        }

        private void HandlePackets()
        {
            try
            {
                NetworkStream stream = ServerWindow.ConnectionManager.TcpClient!.GetStream();
                while (ServerWindow.ConnectionManager.IsConnected)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = Task.Run(async () => await stream.ReadAsync(buffer, 0, buffer.Length)).Result;
                    var stringMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var packet = JsonConvert.DeserializeObject<Packet>(stringMessage);

                    if (packet != null)
                    {
                        OnReceivePackets(packet);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            
        }

        private void RemoteWindow_OnReceivePackets(Packet packet)
        {
            switch(packet.ContentType)
            {
                case Packet.MessageType.Broadcast:
                    _RemoteWindowDataContext.Messages.Add(JsonConvert.DeserializeObject<string>(packet.Payload));
                    break;

                case Packet.MessageType.Frame:
                    _RemoteWindowDataContext.Frame = JsonConvert.DeserializeObject<Bitmap>(packet.Payload);
                    break;
            }
        }
    }
}
