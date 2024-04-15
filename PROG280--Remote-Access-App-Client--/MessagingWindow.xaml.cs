using Microsoft.Win32;
using PROG280__Remote_Access_App_Data__;
using PROG2800__Remote_Access_App_Client__.MessagingWindowComponents;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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

namespace PROG280__Remote_Access_App_Client__
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
            Task.Run(client.ReceiveDataPackets);
            //AskForDisplayName();
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
            btnSendFiles.IsEnabled = false;

            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.ShowDialog();

                if (ofd != null)
                {
                    await client.SendDataPacket(MessageType.FileChunk , ofd.SafeFileName);

                    await Task.WhenAny(Task.Delay(10000));

                    if (client.SendingFile)
                    {
                        await SendFileData(File.ReadAllBytes(ofd.FileName));
                        client.SendingFile = false;
                    }

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

        private async Task SendFileData(byte[] fileData)
        {
            NetworkConnected client = (NetworkConnected)DataContext;

            int totalChunks = (int)Math.Ceiling((double)fileData.Length / (double)1024);
            int chunkIndex = 0;

            while (chunkIndex != totalChunks)
            {
                int offset = chunkIndex * client!.ChunkSize;
                int length = Math.Min(client!.ChunkSize, fileData.Length - offset);
                byte[] chunk = new byte[client!.ChunkSize];
                Buffer.BlockCopy(fileData, offset, chunk, 0, length);

                // Serialize packet and send
                await client!.SendDataPacket(MessageType.FileChunk, chunk);
                chunkIndex++;
            }

            await client!.SendDataPacket(MessageType.FileEnd, new byte[client.ChunkSize]);
        }

        private async void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            NetworkConnected client = (NetworkConnected)DataContext;
            client.ChatMessages.Add($"{client.DisplayName}: {txtMessage.Text}");
            await client.SendDataPacket(MessageType.Message, txtMessage.Text);
            txtMessage.Text = "";
        }
    }
}
