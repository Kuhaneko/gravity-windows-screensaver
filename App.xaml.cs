﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GravityWindows
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        List<GravityScreen> drawOrder;
        bool minigame = false;
        bool minigameUnlocked = false;
        string passphrase = "";
        HashSet<char> lettersToGuess;
        List<char> guessedLetters;
        TextBlock passphraseTextBlock;
        TextBlock guessedLettersTextBlock;

        public App(string[] args)
        {

            if (args.Length > 0)
            {

                switch (args[0])
                {
                    case "/s":
                        CreateAdditionalWindows();
                        break;
                    case "/c":
                        SettingsWindow settingsWindow = new SettingsWindow();
                        settingsWindow.Show();
                        break;
                    case "/p":
                        Application.Current.Shutdown();
                        break;
                    default:
                        Application.Current.Shutdown();
                        break;
                }

            }
            else
            {
                SettingsWindow settingsWindow = new SettingsWindow();
                settingsWindow.Show();
            }
        }

        void CreateFailsafeWindows()
        {
            Console.WriteLine("FAILSAFE, PLEASE RUN CONFIG FIRST!!");
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;

            for (int i = 0; i < screens.Length; i++)
            {


                System.Drawing.Bitmap bmap = new System.Drawing.Bitmap(screens[i].Bounds.Width, screens[i].Bounds.Height);
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmap);
                g.FillRegion(System.Drawing.Brushes.Black, new System.Drawing.Region(screens[i].Bounds));
                
                MemoryStream memory = new MemoryStream();
                bmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                Image image = new Image
                {
                    Margin = new Thickness(0, 0, 0, 0),
                    Width = screens[i].Bounds.Width,
                    Height = screens[i].Bounds.Height,
                    Source = bitmapImage,
                };


                Window additionalWindow = new Window
                {
                    Title = $"Additional Window {i}",
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    WindowState = WindowState.Normal,
                    Top = screens[i].Bounds.Top,
                    Left = screens[i].Bounds.Left,
                    Width = screens[i].Bounds.Width,
                    Height = screens[i].Bounds.Height,
                    WindowStyle = WindowStyle.None
                };
                additionalWindow.KeyDown += KeyPressed;

                Grid grid = new Grid();
                Label label = new Label();
                label.Content = new TextBlock { Text = "No config present, run config first\n\nPress ESC to exit", TextAlignment = TextAlignment.Center };
                label.Margin = new Thickness(screens[i].Bounds.Width * 0.3, screens[i].Bounds.Height * 0.4, screens[i].Bounds.Width * 0.3, screens[i].Bounds.Height * 0.4);
                label.Foreground = System.Windows.Media.Brushes.White;
                label.FontSize = (int)(screens[i].Bounds.Height / 24);
                label.HorizontalContentAlignment = HorizontalAlignment.Center;
                label.VerticalContentAlignment = VerticalAlignment.Center;
                grid.Children.Add(image);
                grid.Children.Add(label);
               
                additionalWindow.Content = grid;

                additionalWindow.Show();
                additionalWindow.WindowState = WindowState.Maximized;
            }
        }
        void CreateAdditionalWindows()
        {
            //Step 1. - Prep path
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = path + "\\GravityWindows\\";
            if (!Directory.Exists(path)) //doesn't exist then fail
            {
                CreateFailsafeWindows();
                return;
            }
            else //if exists then check pick random path
            {
                string[] folders = Directory.GetDirectories(path);
                if (folders.Length > 0)
                {
                    Random rand = new Random();
                    path = path + $"win{rand.Next(folders.Length)+1}\\";
                }
                else
                {
                    CreateFailsafeWindows();
                    return;
                }
            }


            //Step 2 - Load MetaData
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            drawOrder = new List<GravityScreen>();
            string jsonText = System.IO.File.ReadAllText(path + "config.json");
            AppMeta winMeta = serializer.Deserialize<AppMeta>(jsonText);
            if (winMeta == null)
            {
                CreateFailsafeWindows();
                return;
            }

            //Step 2.5 Load screens and check if monitors match the bounds
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            Console.WriteLine("TODO: Prep monitor check");


            //Step 3 - Prep passphrase minigame
            minigame = winMeta.passwordEnabled;
            if (minigame)
            {
                passphrase = winMeta.passphrase;
                lettersToGuess = new HashSet<char>();
                guessedLetters = new List<char>();

                string removeUnguessable = Regex.Replace(winMeta.passphrase.ToLower(), @"[^a-zA-Z]", "");
                foreach (char item in removeUnguessable)
                {
                    lettersToGuess.Add(item);
                }
            }
            //Step 4 - Reconstruct the windows
            for (int i = 0; i < screens.Length; i++)
            {
                GravityScreen gs = new GravityScreen(screens[i].Bounds, i == 0 ? true : false);
                drawOrder.Add(gs);
                Grid canvas = new Grid();
                if (minigame)
                {
                    if(i==0) //primary screen only
                    {
                        BitmapSource passphraseHintImage = new BitmapImage(new Uri(path + "passphrase_hint.png"));
                        if (passphraseHintImage.PixelWidth == screens[0].Bounds.Width && passphraseHintImage.PixelHeight == screens[0].Bounds.Height)
                        {
                            Image passphrase_hint = new Image
                            {
                                Margin = new Thickness(0, 0, 0, 0),
                                Width = screens[i].Bounds.Width,
                                Height = screens[i].Bounds.Height,
                                Source = passphraseHintImage
                            };
                            canvas.Children.Add(passphrase_hint);

                            Label passphraseLabel = new Label();
                            string phassphraseBuilder = "";
                            bool ignoreFirstChar = true;
                            for (int charIndex = 0; charIndex < passphrase.Length; charIndex++)
                            {
                                if (!ignoreFirstChar) phassphraseBuilder += " ";
                                else ignoreFirstChar = false;
                                if (char.IsLetter(passphrase[charIndex])) phassphraseBuilder += "_";
                                else phassphraseBuilder += passphrase[charIndex];
                            }

                            passphraseTextBlock = new TextBlock { Text = phassphraseBuilder, TextAlignment = TextAlignment.Center };
                            passphraseLabel.Content = passphraseTextBlock;
                            passphraseLabel.Margin = new Thickness(screens[i].Bounds.Width * 0.2, screens[i].Bounds.Height * 0.75, screens[i].Bounds.Width * 0.2, screens[i].Bounds.Height * 0.15);
                            passphraseLabel.Foreground = System.Windows.Media.Brushes.White;
                            passphraseLabel.FontSize = (int)(screens[i].Bounds.Height / 24);
                            passphraseLabel.HorizontalContentAlignment = HorizontalAlignment.Center;
                            passphraseLabel.VerticalContentAlignment = VerticalAlignment.Center;
                            canvas.Children.Add(passphraseLabel);


                            Label guessedLettersLabel = new Label();
                            guessedLettersTextBlock = new TextBlock { Text = "Guessed characters:\n\n", TextAlignment = TextAlignment.Center };
                            guessedLettersLabel.Content = guessedLettersTextBlock;
                            guessedLettersLabel.Margin = new Thickness(screens[i].Bounds.Width * 0.75, screens[i].Bounds.Height * 0.2, screens[i].Bounds.Width * 0.05, screens[i].Bounds.Height * 0.2);
                            guessedLettersLabel.Foreground = System.Windows.Media.Brushes.White;
                            guessedLettersLabel.FontSize = (int)(screens[i].Bounds.Height / 32);
                            guessedLettersLabel.HorizontalContentAlignment = HorizontalAlignment.Center;
                            guessedLettersLabel.VerticalContentAlignment = VerticalAlignment.Top;
                            canvas.Children.Add(guessedLettersLabel);

                        }
                        else
                        {
                            //failover
                            minigame = false;
                        }
                    }
                }
                
                //while this is less efficient if minigame ever fails it will cancel out of anything it did and fall back to this...
                //probably safer to do it this way anyway...
                if(!minigame || i != 0)
                {
                    //Step 4.1 - Black background
                    System.Drawing.Bitmap bmap = new System.Drawing.Bitmap(screens[i].Bounds.Width, screens[i].Bounds.Height);
                    System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmap);
                    g.FillRegion(System.Drawing.Brushes.Black, new System.Drawing.Region(screens[i].Bounds));

                    MemoryStream memory = new MemoryStream();
                    bmap.Save(memory, ImageFormat.Bmp);
                    memory.Position = 0;

                    BitmapImage blackBitmap = new BitmapImage();
                    blackBitmap.BeginInit();
                    blackBitmap.StreamSource = memory;
                    blackBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    blackBitmap.EndInit();

                    Image blackBackground = new Image
                    {
                        Margin = new Thickness(0, 0, 0, 0),
                        Width = screens[i].Bounds.Width,
                        Height = screens[i].Bounds.Height,
                        Source = blackBitmap,
                    };
                    canvas.Children.Add(blackBackground);
                    if (!minigame)
                    {
                        //Step 4.2 - Minigame / Regular background
                        Label label = new Label();
                        label.Content = new TextBlock { Text = "Press ESC to exit", TextAlignment = TextAlignment.Center };
                        label.Margin = new Thickness(screens[i].Bounds.Width * 0.3, screens[i].Bounds.Height * 0.4, screens[i].Bounds.Width * 0.3, screens[i].Bounds.Height * 0.4);
                        label.Foreground = System.Windows.Media.Brushes.White;
                        label.FontSize = (int)(screens[i].Bounds.Height / 24);
                        label.HorizontalContentAlignment = HorizontalAlignment.Center;
                        label.VerticalContentAlignment = VerticalAlignment.Center;
                        canvas.Children.Add(label);
                    }
                }

                //Step 4.3 - Image loading! Desktop comes first
                Image desktop = new Image
                {
                    Margin = new Thickness(0, 0, 0, 0),
                    Width = screens[i].Bounds.Width,
                    Height = screens[i].Bounds.Height,
                    Source = (ImageSource)new ImageSourceConverter().ConvertFromString(path + $"screen{i}\\desktop.png")
                };
                canvas.Children.Add(desktop);
                gs.windows.Add(new GravityWindow(desktop, i, true));

                List<AppWindowMeta> windowList = winMeta.appScreens[i].AppWindows;
                windowList.Sort((x, y) => x.zOrder.CompareTo(y.zOrder));
                //Step 4.2 - Reconstruct all windows based on ID
                foreach (AppWindowMeta appWindow in windowList)
                {
                    //4.3 - Verify if image exists
                    if (!File.Exists(path + $"screen{i}\\window{appWindow.ID}.png")) continue;

                    Rect normalizedRect = NormalizeRect(winMeta.appScreens[i].screenRect, appWindow.rect);
                    Image windowImage = new Image
                    {
                        //left top right bottom
                        Margin = new Thickness(normalizedRect.Left, normalizedRect.Top, 0, 0),
                        Width = appWindow.rect.Right - appWindow.rect.Left,
                        Height = appWindow.rect.Bottom - appWindow.rect.Top,
                        Source = (ImageSource)new ImageSourceConverter().ConvertFromString(path + $"screen{i}\\window{appWindow.ID}.png"),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                    };
                    canvas.Children.Add(windowImage);
                    gs.windows.Add(new GravityWindow(windowImage, i));
                }

                gs.windows.Reverse();

                Window additionalWindow = new Window
                {
                    Title = $"Additional Window {i}",
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

                additionalWindow.Content = canvas;
                //Step 5 - Prep update function

            }
                _stopwatch = new Stopwatch();
                gravityWindows = new List<GravityWindow>();
                CompositionTarget.Rendering += Update;


            /*

             * Height="1080"

             * Width="1920"

             * ResizeMode="NoResize"

             * WindowState="Maximized"

             * MouseDown="AnyMouseButtonPressed"

             * MouseMove="MouseMoved"/>

            */

        }

        Stopwatch _stopwatch;
        int firstFrame = 0;
        double deltaTime = 0;
        double timeElapsed = 0;
        List<GravityWindow> gravityWindows;
        double gravity = 9.81;

        // Called just before frame is rendered to allow custom drawing.
        protected void Update(object sender, EventArgs e)
        {
            if (firstFrame == 0)
            {
                firstFrame++;
                // Starting timing.
                _stopwatch.Start();
            }
            deltaTime = (float)_stopwatch.Elapsed.TotalSeconds - timeElapsed;
            timeElapsed = (float)_stopwatch.Elapsed.TotalSeconds;

            for (int i = 0; i < gravityWindows.Count; i++)
            {
                GravityWindow gravityWindow = gravityWindows[i];
                //FLIP THOSE TWO AROUND
                gravityWindow.velocity = gravityWindow.velocity + gravity * deltaTime;
                gravityWindow.image.Margin = new Thickness(gravityWindow.image.Margin.Left, gravityWindow.image.Margin.Top + gravityWindow.velocity, 0, 0);


                if (gravityWindow.image.Margin.Top > (drawOrder[gravityWindow.belongsToScreen].rect.Bottom - drawOrder[gravityWindow.belongsToScreen].rect.Top) + 50)
                {
                    gravityWindows.Remove(gravityWindow);
                }
            }

        }

        private Rect NormalizeRect(Rect screenRect, Rect appRect)
        {
            if (screenRect.Top == 0 && screenRect.Left == 0) return appRect;

            Rect r = new Rect();
            r.Top = appRect.Top + (screenRect.Top * -1);
            r.Bottom = appRect.Bottom + (screenRect.Bottom * -1);
            r.Left = appRect.Left + (screenRect.Left * -1);
            r.Right = appRect.Right + (screenRect.Right * -1);
            return r;
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                //Application.Current.Shutdown();
                WakeUp();
            }

            if(minigameUnlocked)
            {
                if (e.Key >= Key.A && e.Key <= Key.Z)
                {
                    char keyChar = (char)('A' + (e.Key - Key.A));
                    keyChar = char.ToLower(keyChar);
                    if (lettersToGuess.Contains(keyChar))
                    {
                        lettersToGuess.Remove(keyChar);
                        string phassphraseBuilder = "";
                        bool ignoreFirstChar = true;
                        for (int charIndex = 0; charIndex < passphrase.Length; charIndex++)
                        {
                            if (!ignoreFirstChar) phassphraseBuilder += " ";
                            else ignoreFirstChar = false;
                            if (char.IsLetter(passphrase[charIndex]))
                            {
                                if (!lettersToGuess.Contains(char.ToLower(passphrase[charIndex]))) phassphraseBuilder += passphrase[charIndex];
                                else phassphraseBuilder += "_";
                            }
                            else phassphraseBuilder += passphrase[charIndex];
                        }
                        passphraseTextBlock.Text = phassphraseBuilder;
                        if(lettersToGuess.Count == 0) Application.Current.Shutdown();
                    }
                    if (!guessedLetters.Contains(keyChar))
                    {
                        guessedLetters.Add(keyChar);
                        string charsToAddToLabel = "";
                        bool firstIgnore = true;
                        for (int consecLetter = 0; consecLetter < guessedLetters.Count; consecLetter++)
                        {
                            char c = guessedLetters[consecLetter];
                            if (!firstIgnore) charsToAddToLabel += ", ";
                            else firstIgnore = false;
                            //enter break after 7th character
                            if (consecLetter % 8 == 7) charsToAddToLabel += "\n";
                            charsToAddToLabel+=char.ToLower(c);
                        }
                        guessedLettersTextBlock.Text = "Guessed characters:\n\n"+charsToAddToLabel;
                    }
                }
            }

            //WakeUp();
            //Console.WriteLine("KeyPressed");
        }



        private void AnyMouseButtonPressed(object sender, MouseButtonEventArgs e)
        {
            System.Drawing.Point mousePos = System.Windows.Forms.Cursor.Position;
            GravityScreen clickedOnScreen = GetDrawOrderScreenBasedOnPoint(mousePos);
            if (clickedOnScreen != null)
            {
                GravityWindow windowClicked = GetClickedWindowBasedOnPoint(mousePos, clickedOnScreen);
                if (windowClicked != null)
                {
                    if (gravityWindows.IndexOf(windowClicked) <= 0)
                    {
                        gravityWindows.Add(windowClicked);
                        if (clickedOnScreen.mainScreen && windowClicked.desktop) minigameUnlocked = true; 
                    }
                }
            }
        }

        private GravityScreen GetDrawOrderScreenBasedOnPoint(System.Drawing.Point pos)
        {
            foreach (GravityScreen screen in drawOrder)
            {
                if (pos.X >= screen.rect.Left && pos.X < screen.rect.Right && pos.Y >= screen.rect.Top && pos.Y < screen.rect.Bottom) return screen;
            }
            return null;
        }

        private GravityWindow GetClickedWindowBasedOnPoint(System.Drawing.Point pos, GravityScreen screen)
        {
            Rect mouseRect = new Rect();
            mouseRect.Left = pos.X;
            mouseRect.Top = pos.Y;
            Rect normalizeMousePos = NormalizeRect(screen.rect, mouseRect);
            for (int i = 0; i < screen.windows.Count; i++)
            {
                if (normalizeMousePos.Left >= screen.windows[i].image.Margin.Left &&
                    normalizeMousePos.Left < screen.windows[i].image.Margin.Left + screen.windows[i].image.Width &&
                    normalizeMousePos.Top >= screen.windows[i].image.Margin.Top &&
                    normalizeMousePos.Top < screen.windows[i].image.Margin.Top + screen.windows[i].image.Height) return screen.windows[i];
            }
            return null;
        }


        private void MouseMoved(object sender, MouseEventArgs e)
        {
            //WakeUp();
        }

        void WakeUp()
        {
            Application.Current.Shutdown();
        }
    }
}
