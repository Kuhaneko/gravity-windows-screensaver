using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GravityWindows
{
    public class AppScreenMeta
    {
        public List<AppWindowMeta> AppWindows;
        public Rect screenRect;

        public AppScreenMeta(Rect rect) 
        { 
            AppWindows = new List<AppWindowMeta>();
            screenRect = rect;
        }

        public AppScreenMeta(Rectangle bounds)
        {
            AppWindows = new List<AppWindowMeta>();
            screenRect = new Rect();
            screenRect.Left = bounds.Left;
            screenRect.Top = bounds.Top;
            screenRect.Right = bounds.Right;
            screenRect.Bottom = bounds.Bottom;
        }

        /// <summary>
        /// dont use this!!
        /// </summary>
        public AppScreenMeta()
        {
        }

        public void AddWindow(AppWindowMeta appWindowMeta)
        {
            AppWindows.Add(appWindowMeta);
        }

        public List<AppWindowMeta> GetAppWindows()
        {
            return AppWindows;
        }


        public bool IsWindowInScreen(Rect windowRect)
        {
            return windowRect.Top > screenRect.Top - 50 && windowRect.Bottom < screenRect.Bottom + 50 && windowRect.Left > screenRect.Left - 50 && windowRect.Right < screenRect.Right + 50;
        }
    }
}
