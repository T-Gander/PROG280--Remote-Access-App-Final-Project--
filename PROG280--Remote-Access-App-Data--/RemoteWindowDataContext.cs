using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PROG280__Remote_Access_App_Data__
{
    public class RemoteWindowDataContext
    {
        public Image Frame { get; set; } = new();

        public ObservableCollection<string> Messages { get; set; } = new();
    }
}
