using System.Windows.Controls;

namespace GravityWindows
{
    public class GravityWindow
    {
        public double velocity;
        public Image image;
        public int belongsToScreen = -1;
        public GravityWindow(Image image, int belongsToScreen)
        {
            this.image = image;
            this.belongsToScreen = belongsToScreen;
        }
    }
}
