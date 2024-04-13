using Microsoft.Win32;
using PROG280__Remote_Access_App_Data__;
using PROG2800__Remote_Access_App_Client__.MessagingWindowComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using static PROG280__Remote_Access_App_Data__.Packet;

namespace PROG2800__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for MessagingWindow.xaml
    /// </summary>
    public partial class MessagingWindow : Window
    {
        public MessagingWindow(NetworkConnected client)
        {
            InitializeComponent();
            DataContext = client;
            Task.Run(client.ReceivePackets);
            AskForDisplayName();
        }

        private void AskForDisplayName()
        {
            DisplayName _displayNameWindow = new((NetworkConnected)DataContext);
            _displayNameWindow.ShowDialog();
        }

        //A way of sending files for the Client.

        //A way to control the server PC.

        

        private void btnRequestControl_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnSendFiles_Click(object sender, RoutedEventArgs e)
        {
            NetworkConnected client = (NetworkConnected)DataContext;

            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.ShowDialog();

                if (ofd != null)
                {
                    byte[] fileData = File.ReadAllBytes(ofd.FileName);

                    //var sendFile = await client.AskToSendFile(fileData);

                    //btnSendFiles.IsEnabled = false;

                    //if (sendFile)
                    //{
                    //    await _AppType.ReceiveFiles();
                    //}

                    btnSendFiles.IsEnabled = true;
                }
                else
                {
                    return;
                }
            }
            catch
            {
                //Something bad happened
            }
            
        }

        private async void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            NetworkConnected client = (NetworkConnected)DataContext;
            client.ChatMessages.Add(txtMessage.Text);
            await client.SendPacket(MessageType.Message, txtMessage.Text);
        }
    }
}
