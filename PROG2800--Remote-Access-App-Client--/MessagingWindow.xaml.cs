using PROG280__Remote_Access_App_Data__;
using System;
using System.Collections.Generic;
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

namespace PROG2800__Remote_Access_App_Client__
{
    /// <summary>
    /// Interaction logic for MessagingWindow.xaml
    /// </summary>
    public partial class MessagingWindow : Window
    {
        private NetworkConnected _AppType;

        public MessagingWindow(NetworkConnected appType)
        {
            InitializeComponent();
            DataContext = appType;
            _AppType = appType;
            appType.ReceiveMessages();
        }

        //A way of listening for messages for both Client and Server.

        //A way of sending files for the Client.

        //A way to control the server PC.

        

        private void btnRequestControl_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSendFiles_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            await _AppType.SendMessage(txtMessage.Text);
        }
    }
}
