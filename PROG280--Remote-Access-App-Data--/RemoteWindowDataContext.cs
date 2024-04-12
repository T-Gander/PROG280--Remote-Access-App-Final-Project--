using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PROG280__Remote_Access_App_Data__
{
    public class RemoteWindowDataContext : System.ComponentModel.INotifyPropertyChanged
    {
        public RemoteWindowDataContext(string ip) 
        {
            RemoteIP = ip;
        }

        private BitmapImage? _frame;

        public BitmapImage? Frame
        {
            get { return _frame; }
            set
            {
                _frame = value;
                OnPropertyChanged(nameof(Frame));
            }
        }

        public string RemoteIP { get; set; }

        public ObservableCollection<string> Messages { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
