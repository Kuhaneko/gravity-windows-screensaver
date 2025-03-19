using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Shell32;

namespace GravityWindows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private string loadedImagePath = null;
        const short SWP_NOMOVE = 0X2;
        const short SWP_NOSIZE = 1;
        const short SWP_NOZORDER = 0X4;
        const int SWP_SHOWWINDOW = 0x0040;
        const long WS_MAXIMIZE = 0x01000000L;

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);
        [Flags]
        private enum DwmWindowAttribute : uint
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_LAST
        }
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();


        private enum GWL
        {
            GWL_WNDPROC = (-4),
            GWL_HINSTANCE = (-6),
            GWL_HWNDPARENT = (-8),
            GWL_STYLE = (-16),
            GWL_EXSTYLE = (-20),
            GWL_USERDATA = (-21),
            GWL_ID = (-12)
        }

        public static int GetZOrder(IntPtr hWnd)
        {
            int z = 0;
            // 3 is GetWindowType.GW_HWNDPREV
            for (IntPtr h = hWnd; h != IntPtr.Zero; h = (IntPtr)GetWindowLong(h, 3)) z++;
            return z;
        }


        bool isWindowMaximized(IntPtr handle)
        {
            int style = GetWindowLong(handle, (int)GWL.GWL_STYLE);
            return ((style & WS_MAXIMIZE) == WS_MAXIMIZE);
        }

        static Rect GetWindowRectangle(IntPtr hWnd)
        {
            Rect rect;

            int size = Marshal.SizeOf(typeof(Rect));
            DwmGetWindowAttribute(hWnd, (int)DwmWindowAttribute.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, size);

            return rect;
        }


        public SettingsWindow()
        {
            InitializeComponent();
            Setup();
        }

        private void Setup()
        {
            CheckBox_Checked(null,null);
            loading_text.Visibility = Visibility.Hidden;
            ResizeMode = ResizeMode.NoResize;
        }


        private void CaptureButtonClick(object sender, RoutedEventArgs e)
        {
            loading_text.Visibility = Visibility.Visible;
            loading_text.Foreground = System.Windows.Media.Brushes.Red;
            loading_text.Content = "Loading...";
            //Step 0. Config the pictures folder for the project
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = path + "\\GravityWindows\\";
            if (!Directory.Exists(path)) //doesn't exist thus set up
            {
                Directory.CreateDirectory(path);
                path = path + "win1\\";
                Directory.CreateDirectory(path);
            }
            else //if exists then check what's been done
            {
                string[] folders =  Directory.GetDirectories(path);
                path = path + $"win{folders.Length+1}\\";
                Directory.CreateDirectory(path);
            }


            //Step 1. Create data for screens of current system
            Screen[] screens = Screen.AllScreens;
            AppMeta appMeta = new AppMeta();
            List<AppScreenMeta> appScreens = appMeta.appScreens;
            foreach (Screen screen in screens)
            {
                appMeta.appScreens.Add(new AppScreenMeta(screen.Bounds));
            }

            //Step 1.5 - Config passphrase
            if((passphrase_checkbox.IsChecked ?? false) && passphrase_input.Text != "")
            {
                if (loadedImagePath != null && loadedImagePath != "") {
                    File.Copy(loadedImagePath, path + "passphrase_hint.png");
                    appMeta.passphrase = passphrase_input.Text;
                    appMeta.passwordEnabled = true;
                }
            }

            //Step 2. For each window
            foreach (KeyValuePair<IntPtr, string> window in OpenWindowGetter.GetOpenWindows())
            {
                //Step 2.1 Set them up for analysis
                IntPtr handle = window.Key;
                Rect rect = GetWindowRectangle(handle);

                //Step 2.2 Add the window to the correct screen system
                foreach (AppScreenMeta appScreen in appScreens)
                {
                    if (appScreen.IsWindowInScreen(rect))
                    {
                        appScreen.AddWindow(new AppWindowMeta(GetZOrder(handle),rect, isWindowMaximized(handle), window.Value, handle, appScreen.AppWindows.Count));
                        break;
                    }
                }
            }

            //Step 2.3 - Prep step 3 by getting me the handle of main window
            IntPtr MainAppWindow = GetForegroundWindow();
            Rect MainAppWindowOriginalRect = GetWindowRectangle(MainAppWindow);
            //Step 3. Sort - best effort to screenshot in the reverse order
            for (int screenCount = 0; screenCount < appScreens.Count; screenCount++)
            {
                AppScreenMeta screen = appScreens[screenCount];
                screen.AppWindows.Sort((x,y) => x.zOrder.CompareTo(y.zOrder));
                screen.AppWindows.Reverse();
                Directory.CreateDirectory(path+$"screen{screenCount}\\");

                //Step 3.1 - Capture Desktop
                Shell shellObject = new Shell();
                shellObject.ToggleDesktop();
                Thread.Sleep(500);
                CaptureRect(screen.screenRect).Save(path + $"screen{screenCount}\\desktop.png", ImageFormat.Png);
                shellObject.ToggleDesktop();
                Thread.Sleep(500);

                //Step 4. Since sorted and we're in the loop it's screenshot time
                foreach (AppWindowMeta app in screen.AppWindows)
                {
                    //Step 4.1 - Bring app in front of everybody else
                    SetForegroundWindow(app.GetHandle());
                    SetWindowPos(MainAppWindow, 0, screen.screenRect.Right+50, screen.screenRect.Bottom + 50, MainAppWindowOriginalRect.Right-MainAppWindowOriginalRect.Left, MainAppWindowOriginalRect.Bottom - MainAppWindowOriginalRect.Top, SWP_NOZORDER | SWP_SHOWWINDOW);
                    SetForegroundWindow(MainAppWindow);
                    Thread.Sleep(500);
                    CaptureWindow(app.GetHandle()).Save(path+$"screen{screenCount}\\window{app.ID}.png",ImageFormat.Png);
                }
            }
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(appMeta);
            File.WriteAllText(path+"config.json", json);

            //Step 5 - Restore main window
            SetForegroundWindow(MainAppWindow);
            SetWindowPos(MainAppWindow, 0, MainAppWindowOriginalRect.Left , MainAppWindowOriginalRect.Top, MainAppWindowOriginalRect.Right - MainAppWindowOriginalRect.Left, MainAppWindowOriginalRect.Bottom - MainAppWindowOriginalRect.Top, SWP_NOZORDER | SWP_SHOWWINDOW);
            loading_text.Foreground = System.Windows.Media.Brushes.Green;
            loading_text.Content = "Done!";
        }

        public static Bitmap CaptureWindow(IntPtr handle)
        {
            Rect rect = GetWindowRectangle(handle);
            var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            var result = new Bitmap(bounds.Width, bounds.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.CopyFromScreen(new System.Drawing.Point(bounds.Left, bounds.Top), System.Drawing.Point.Empty, bounds.Size);
            }

            return result;
        }

        public static Bitmap CaptureRect(Rect rect)
        {
            var bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            var result = new Bitmap(bounds.Width, bounds.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.CopyFromScreen(new System.Drawing.Point(bounds.Left, bounds.Top), System.Drawing.Point.Empty, bounds.Size);
            }

            return result;
        }

        private void Passphrase_FileSelect(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "PNG Files (*.png)|*.png" };
            var result = ofd.ShowDialog();
            if (result == false) return;
            ImageSource loadedImage = (ImageSource)new ImageSourceConverter().ConvertFromString(ofd.FileName);
            Rectangle mainScreenBounds = Screen.AllScreens[0].Bounds;
            int imageCompareWidth = (int)(loadedImage.Width - (mainScreenBounds.Right - mainScreenBounds.Left));
            int imageCompareHeight = (int)(loadedImage.Height - (mainScreenBounds.Bottom - mainScreenBounds.Top));
            imageCompareWidth = imageCompareWidth < 0 ? imageCompareWidth * -1 : imageCompareWidth;
            imageCompareHeight = imageCompareHeight < 0 ? imageCompareHeight * -1 : imageCompareHeight;


            if (imageCompareWidth >= 0 && imageCompareWidth < 5 && imageCompareHeight >= 0 && imageCompareHeight < 5 )
            {
                loadedImagePath = ofd.FileName;
                passphrase_hintImage.Source = loadedImage;
                passphrase_resWarning.Foreground = System.Windows.Media.Brushes.Black;
            }
            else
            {
                passphrase_resWarning.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            bool hintEnabled = passphrase_checkbox.IsChecked ?? false;
            passphrase_input.Visibility = hintEnabled ? Visibility.Visible : Visibility.Hidden;
            passphrase_hintFileSelect.Visibility = hintEnabled ? Visibility.Visible : Visibility.Hidden;
            passphrase_hintImage.Visibility = hintEnabled ? Visibility.Visible : Visibility.Hidden;
            passphrase_resWarning.Visibility = hintEnabled ? Visibility.Visible : Visibility.Hidden;
            passphrase_prompt.Visibility = hintEnabled ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
