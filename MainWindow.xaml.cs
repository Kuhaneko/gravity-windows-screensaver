using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ScreenSaver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        int mouseMovedHack = 0;

        public MainWindow()
        {

            //InitializeComponent();

            CreateAdditionalWindows();

        }

        void CreateAdditionalWindows()
        {
            //System.Windows.

            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            mouseMovedHack = screens.Length;

            for (int i = 0; i < screens.Length; i++)
            {
                System.Drawing.Bitmap bmap = new System.Drawing.Bitmap(screens[i].Bounds.Width, screens[i].Bounds.Height);
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmap);
                g.CopyFromScreen(System.Drawing.Point.Empty, System.Drawing.Point.Empty, screens[i].Bounds.Size);
                MemoryStream memory = new MemoryStream();
                bmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bmapimg = new BitmapImage();
                bmapimg.BeginInit();
                bmapimg.StreamSource = memory;
                bmapimg.CacheOption = BitmapCacheOption.OnLoad;
                bmapimg.EndInit();

                Image image = new Image
                {
                    Margin = new Thickness(0, 0, 0, 0),
                    Width = screens[i].Bounds.Width,
                    Height = screens[i].Bounds.Height,
                    Source = bmapimg
                };

                Console.WriteLine($"Creating window {i}, dims: {screens[i].Bounds.Width}x{screens[i].Bounds.Height}");
                Window additionalWindow = new Window
                {
                    Title = "Additional Window 1",
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    WindowState = WindowState.Normal,
                    Top = screens[i].Bounds.Top,
                    Left = screens[i].Bounds.Left,
                    Width = screens[i].Bounds.Width,
                    Height = screens[i].Bounds.Height,
                    WindowStyle = WindowStyle.None
                };

                additionalWindow.KeyDown += KeyPressed;
                additionalWindow.MouseDown += AnyMouseButtonPressed;
                additionalWindow.MouseMove += MouseMoved;
                additionalWindow.Show();
                additionalWindow.WindowState = WindowState.Maximized;

                additionalWindow.Content = image;
            }


            /*

             * Height="1080"

             * Width="1920"

             * ResizeMode="NoResize"

             * WindowState="Maximized"

             * MouseDown="AnyMouseButtonPressed"

             * MouseMove="MouseMoved"/>

            */

        }


        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }

            WakeUp();
            Console.WriteLine("KeyPressed");
        }



        private void AnyMouseButtonPressed(object sender, MouseButtonEventArgs e)
        {
            WakeUp();
            Console.WriteLine("MouseButton");
        }


        private void MouseMoved(object sender, MouseEventArgs e)
        {
            mouseMovedHack--;
            if (mouseMovedHack <= 0)
            {
                WakeUp();
                Console.WriteLine("MouseMoved");
            }
        }

        void WakeUp()
        {
            //Application.Current.Shutdown();
        }
    }
}