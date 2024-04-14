using Newtonsoft.Json;
using PROG280__Remote_Access_App_Data__;
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
        public RemoteWindow(NetworkConnected client)
        {
            InitializeComponent();
            DataContext = this;
            //Task.Run(TestVideo);
            Task.Run(async () =>
            {
                while (true)
                {
                    BitmapImage? frame = await InitializeFrame();

                    if (frame != null)
                    {
                        Frame = frame;
                    }

                    client.FrameReady = false;
                }
            });
        }

        private async Task<BitmapImage?> InitializeFrame()
        {
            NetworkConnected client = (NetworkConnected)DataContext;

            var ready = await client.IsFrameReady();
                
            if(ready)
            {
                byte[] bitmapBytes = client.FrameChunks.ToArray();
                client.FrameChunks.Clear();

                BitmapImage? frame;

                using (MemoryStream mstream = new(bitmapBytes))
                {
                    frame = new BitmapImage();
                    frame.BeginInit();
                    frame.StreamSource = mstream;
                    frame.CacheOption = BitmapCacheOption.OnLoad;
                    frame.EndInit();
                }
                return frame;
            }
            else
            {
                return null;
            }
        }

        public static readonly DependencyProperty FrameProperty =
            DependencyProperty.Register("Frame", typeof(BitmapImage), typeof(RemoteWindow));

        public BitmapImage Frame
        {
            get { return (BitmapImage)GetValue(FrameProperty); }
            set { SetValue(FrameProperty, value); }
        }
    }
}
