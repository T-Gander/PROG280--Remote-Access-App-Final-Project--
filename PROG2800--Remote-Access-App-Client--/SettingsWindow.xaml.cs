using PROG280__Remote_Access_App_Client__;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(ServerWindow window)
        {
            InitializeComponent();
            DataContext = window;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
