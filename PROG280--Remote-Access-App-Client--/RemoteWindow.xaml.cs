using Newtonsoft.Json;
using PROG280__Remote_Access_App_Client__.Data;
using PROG280__Remote_Access_App_Data__;
using PROG2800__Remote_Access_App_Client__.MessagingWindowComponents;
using SharpHook;
using SharpHook.Native;
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
using System.Windows.Threading;

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
            frame.MouseDown += Frame_MouseDown;
        }

        private async void HandlePackets()
        {
            try
            {
                bool continueHandling = true;

                while (continueHandling)
                {
                    var timeout = Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                    });

                    var videoFeed = Task.Run(async () =>
                    {
                        BitmapImage? frame = await _Client.ReceiveVideoPackets();

                        if (!timeout.IsCompleted)
                        {
                            await Dispatcher.Invoke(async () =>
                            {
                                _RemoteWindowDataContext.Frame = frame;
                            });
                        }
                    });

                    await Task.WhenAny(timeout, videoFeed);

                    if (timeout.IsCompleted)
                    {
                        continueHandling = false;
                    }
                }

                MessageBox.Show("No Video feed, remote user may have disconnected. \n \n Please try reconnecting.","Timeout reached",MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                //Something bad happened
            }
        }

        private async void Frame_MouseDown(object sender, MouseButtonEventArgs mouseEvent)
        {
            System.Windows.Point mousePosition = mouseEvent.GetPosition(this);

            double xRatio = (double)mousePosition.X / (double)frame.ActualWidth;
            double yRatio = (double)mousePosition.Y / (double)frame.ActualHeight;

            System.Windows.Point ratioPoint = new System.Windows.Point(xRatio, yRatio);

            MouseData? mouseData = mouseEvent.ChangedButton switch
            {
                System.Windows.Input.MouseButton.Left => new MouseData { MouseLocation = ratioPoint, MouseButton = SharpHook.Native.MouseButton.Button1 },
                System.Windows.Input.MouseButton.Right => new MouseData { MouseLocation = ratioPoint, MouseButton = SharpHook.Native.MouseButton.Button2 },
                System.Windows.Input.MouseButton.Middle => new MouseData { MouseLocation = ratioPoint, MouseButton = SharpHook.Native.MouseButton.Button3 },
                _ => null // Unknown mouse button, don't send
            };

            if (mouseData != null)
            {
                await _Client.SendDataPacket(Packet.MessageType.MouseMove, mouseData);
            }

            //Send a packet to server and set its mouse location.
            //Figure out where in the window you clicked, and if needed where on the image

            //double widthImageDiff = Width - frame.ActualWidth;
            //double heightImageDiff = Height - frame.ActualHeight;
            
            
        }
    }
}
