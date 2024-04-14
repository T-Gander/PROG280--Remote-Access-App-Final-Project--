using Newtonsoft.Json;
using PROG280__Remote_Access_App_Data__;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace PROG280__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for RemoteWindow.xaml
    /// </summary>
    public partial class RemoteWindow : Window
    {
        RemoteWindowDataContext remoteWindowDataContext = new();

        public BitmapImage? Frame 
        { 
            get 
            { 
                return _frame; 
            }
            set
            {
                _frame = value;
                OnPropertyChanged(nameof(Frame));
            }
        }
        private BitmapImage? _frame;

        public RemoteWindow()
        {
            InitializeComponent();
            DataContext = remoteWindowDataContext;
            NetworkConnected.FrameHandler += UpdateFrame;
            //Open a messaging window.
        }

        public void UpdateFrame(BitmapImage frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Frame = frame;
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
    }
}
