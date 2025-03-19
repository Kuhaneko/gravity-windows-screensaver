using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GravityWindows
{
    public class AppWindowMeta
    {
        public int zOrder;
        public Rect rect;
        public bool maximized;
        public int ID = -1;
        string debug_title;
        IntPtr debug_handle;

        public AppWindowMeta(int zOrder, Rect rect, bool maximized, string title, IntPtr debug_handle, int ID)
        {
            this.zOrder = zOrder;
            this.rect = rect;
            this.maximized = maximized;
            debug_title = title;
            this.debug_handle = debug_handle;
            this.ID = ID;
        }

        /// <summary>
        /// dont use this!!
        /// </summary>
        public AppWindowMeta()
        {
        }

        public IntPtr GetHandle() 
        {
            return debug_handle;
            
        }
    }
}
