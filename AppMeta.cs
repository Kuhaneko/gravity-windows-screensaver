using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GravityWindows
{
    public class AppMeta
    {
        public List<AppScreenMeta> appScreens;
        public bool passwordEnabled = false;
        public string passphrase = "";
        public AppMeta() 
        { 
            appScreens = new List<AppScreenMeta>();
        }

    }
}
