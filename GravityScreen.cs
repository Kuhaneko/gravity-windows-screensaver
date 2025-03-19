using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GravityWindows
{
    internal class GravityScreen
    {
        public List<GravityWindow> windows;
        public Rect rect;
        public bool mainScreen = false;
        public GravityScreen(Rect rect, bool mainScreen = false)
        {
            windows = new List<GravityWindow>();
            this.rect = rect;
            this.mainScreen = mainScreen;
        }

        public GravityScreen(Rectangle bounds, bool mainScreen = false)
        {
            windows = new List<GravityWindow>();
            rect = new Rect();
            rect.Left = bounds.Left;
            rect.Top = bounds.Top;
            rect.Right = bounds.Right;
            rect.Bottom = bounds.Bottom;
            this.mainScreen = mainScreen;
        }
    }
}
