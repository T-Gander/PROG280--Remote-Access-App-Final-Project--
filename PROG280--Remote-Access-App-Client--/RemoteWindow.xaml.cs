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
            Task.Run(() => client.ReceivePackets(HandleFrame));
        }

        public static readonly DependencyProperty FrameProperty =
            DependencyProperty.Register("Frame", typeof(BitmapImage), typeof(RemoteWindow));

        public BitmapImage Frame
        {
            get { return (BitmapImage)GetValue(FrameProperty); }
            set { SetValue(FrameProperty, value); }
        }

        private void HandleFrame(BitmapImage frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Frame = frame;
            });
        }
    }
}
