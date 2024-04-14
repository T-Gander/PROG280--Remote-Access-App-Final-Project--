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
    public partial class RemoteWindow : INotifyPropertyChanged
    {
        private NetworkConnected _Client { get; set; }

        public RemoteWindow(NetworkConnected client, bool test)
        {
            InitializeComponent();
            _Client = client;
            DataContext = this;

            Frame = TestFrame().Result;
            Task.Run(Video);
        }

        private Task<BitmapImage?> RetreiveFrameFromClientRemoteWindow()
        {
            return Task.FromResult(_Client.CurrentFrame);
        }

        private async Task Video()
        {
            while (true)
            {
                await Application.Current.Dispatcher.Invoke(async () => Frame = await RetreiveFrameFromClientRemoteWindow());
                await Task.Delay(1000); // Delay between frames
            }
        }

        private Task<BitmapImage?> TestFrame()
        {
            byte[] imageData;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                _Client!.GrabScreen().Save(memoryStream, ImageFormat.Png); // Save the bitmap to the memory stream as PNG format
                imageData = memoryStream.ToArray(); // Get the byte array from the memory stream
            }

            BitmapImage? frame;

            using (MemoryStream mstream = new(imageData))
            {
                frame = new BitmapImage();
                frame.BeginInit();
                frame.StreamSource = mstream;
                frame.CacheOption = BitmapCacheOption.OnLoad;
                frame.EndInit();
            }
            
            return Task.FromResult(frame);
        }

        private BitmapImage? _frame;

        public BitmapImage? Frame
        {
            get 
            { 
                if(_frame == null)
                {
                    return new();
                }

                return _frame;
            }
            set
            {
                _frame = value;
                OnPropertyChanged(nameof(Frame));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
