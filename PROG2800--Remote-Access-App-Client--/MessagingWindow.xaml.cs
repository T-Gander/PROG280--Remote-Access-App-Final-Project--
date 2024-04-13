using PROG280__Remote_Access_App_Data__;
using PROG2800__Remote_Access_App_Client__.MessagingWindowComponents;
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

            Task.WaitAll(AskForDisplayName());
            
            appType.ReceiveMessages();
        }

        private async Task AskForDisplayName()
        {
            var tcs = new TaskCompletionSource<object?>();

            // Use a TaskCompletionSource to create a task that completes when the RemoteWindow is closed

            void DisplayNameWindow_Closed(object sender, EventArgs e)
            {
                tcs.TrySetResult(null); // Signal that the task is completed
            }

            DisplayName _displayNameWindow = new(_AppType);
            _displayNameWindow.Closed += DisplayNameWindow_Closed;
            _displayNameWindow.Show();

            await tcs.Task;
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
