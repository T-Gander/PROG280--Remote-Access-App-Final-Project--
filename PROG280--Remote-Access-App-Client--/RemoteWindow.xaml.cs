﻿using Newtonsoft.Json;
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
        private NetworkConnected _Client { get; set; }
        private bool _Test { get; set; }

        public RemoteWindow(NetworkConnected client, bool test)
        {
            InitializeComponent();
            _Client = client;
            _Test = test;
            DataContext = this;

            while (true)
            {
                Frame = ReadFrame().Result;
            }
            //if (_Test)
            //{
            //    Frame = TestFrame().Result;
            //}
            //else
            //{
            //    Frame = ReadFrame().Result;
            //    //_Client.FrameHandler += AddFrame;
            //}
            
            //Open a messaging window.
        }

        private Task<BitmapImage?> ReadFrame()
        {
            return Task.FromResult(_Client.CurrentFrame);
        }

        private Task<BitmapImage?> TestFrame()
        {
            int i = 0;
            int j = 0;

            while (true)
            {
                if (j < 1)
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

                    if (i < 1)
                    {
                        Bitmap bitmap;
                        using (MemoryStream stream = new MemoryStream(imageData))
                        {
                            bitmap = new Bitmap(stream);
                        }

                        bitmap.Save("something.png", ImageFormat.Png);
                        i++;
                    }

                    return Task.FromResult(frame);
                    //await Task.Delay(1000);
                }
                return null;
            }
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