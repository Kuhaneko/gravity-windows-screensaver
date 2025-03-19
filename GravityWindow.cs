using System.Windows.Controls;

namespace GravityWindows
{
    public class GravityWindow
    {
        public double velocity;
        public Image image;
        public int belongsToScreen = -1;
        public bool desktop = false;

        public GravityWindow(Image image, int belongsToScreen, bool desktop = false)
        {
            this.image = image;
            this.belongsToScreen = belongsToScreen;
            this.desktop = desktop;
        }
    }
}
