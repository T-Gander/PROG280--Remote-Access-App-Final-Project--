using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROG280__Remote_Access_App_Client__.Data
{
    public class MouseData
    {
        public System.Windows.Point MouseLocation { get; set; }

        public SharpHook.Native.MouseButton MouseButton { get; set; }
    }
}
