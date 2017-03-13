/*
 * Copyright (c) 2017, Sebastien Vincent
 *
 * Distributed under the terms of the BSD 3-clause License.
 * See the LICENSE file for details.
 */

using Camera_NET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace photomaton_wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Maximum image to take by cycle.
        /// </summary>
        private readonly int MAX_IMAGES = 3;

        /// <summary>
        /// File name of the file to be written when the MAX_IMAGES is taken.
        /// </summary>
        private readonly string DUMMY_FILENAME = "images\\dummy.dmy";

        /// <summary>
        /// Camera choice.
        /// </summary>
        private CameraChoice m_cameraChoice = new CameraChoice();

        /// <summary>
        /// A transparent window to overlay the DirectShow.NET stream (WindowsForm).
        /// </summary>
        private TransparentWindow m_window = new TransparentWindow();

        /// <summary>
        /// Number of image taken.
        /// </summary>
        private int m_img = 0;

        /// <summary>
        /// Camera index.
        /// </summary>
        private int m_cameraIndex = 0;

        /// <summary>
        /// Current ountdown before shooting a picture.
        /// </summary>
        private int m_cpt = 5;

        /// <summary>
        /// Timer for the countdown.
        /// </summary>
        private System.Windows.Threading.DispatcherTimer m_timer = new System.Windows.Threading.DispatcherTimer();

        /// <summary>
        /// Display timer for message when DUMMY_FILENAME file exists.
        /// </summary>
        private System.Windows.Threading.DispatcherTimer m_displayTimer = new System.Windows.Threading.DispatcherTimer();

        /// <summary>
        /// Current cheat code.
        /// </summary>
        private string m_cheatCode = "";

        /// <summary>
        /// Exit cheat code.
        /// </summary>
        private readonly string CHEATCODE_EXIT = "AK84";

        /// <summary>
        /// Reset cheat code.
        /// </summary>
        private readonly string CHEATCODE_RESET = "AK85";

        /// <summary>
        /// Webcam change cheat code.
        /// </summary>
        private readonly string CHEATCODE_WEBCAM_CHANGE = "AK86";

        /// <summary>
        /// Go to fullscreen cheat code.
        /// </summary>
        private readonly string CHEATCODE_FULLSCREEN = "AK87";

        /// <summary>
        /// Go to normal screen cheat code.
        /// </summary>
        private readonly string CHEATCODE_NORMAL_SCREEN = "AK88";

        /// <summary>
        /// Convert a bitmap to a ImageSource.
        /// 
        /// Taken from http://stackoverflow.com/a/22501616.
        /// </summary>
        /// <param name="bitmap">Bitmap content.</param>
        /// <returns>ImageSource to be used in WPF.</returns>
        public static ImageSource bitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            m_cameraChoice.UpdateDeviceList();

            ResolutionList resolutions = Camera.GetResolutionList(m_cameraChoice.Devices[m_cameraIndex].Mon);
            Resolution max = null;

            // use the maximum resolution
            foreach (Resolution res in resolutions)
            {
                if (max == null)
                {
                    max = res;
                    continue;
                }

                if (res.Width > max.Width)
                {
                    max = res;
                }
            }

            cameraControl.CameraControl.SetCamera(m_cameraChoice.Devices[m_cameraIndex].Mon, max);

            this.KeyDown += CameraControl_KeyDown;
            cameraControl.KeyDown += CameraControl_KeyDown;
            m_window.KeyDown += CameraControl_KeyDown;
            this.Closing += MainWindow_Closing;
            this.Loaded += MainWindow_Loaded;

            m_window.Visibility = Visibility.Hidden;

            m_timer.Tick += M_timer_Tick;
            m_timer.Interval = new TimeSpan(0, 0, 1);

            m_displayTimer.Tick += M_displayTimer_Tick;
            m_displayTimer.Interval = new TimeSpan(0, 0, 1);

            m_displayTimer.Start();
        }

        /// <summary>
        /// Timeout method to the display timer.
        /// </summary>
        /// <param name="sender">Timer.</param>
        /// <param name="e">Argument.</param>
        private void M_displayTimer_Tick(object sender, EventArgs e)
        {
            if(!File.Exists(DUMMY_FILENAME))
            {
                m_displayTimer.Stop();
                m_window.Visibility = Visibility.Hidden;
            }
            else
            {
                m_window.setText("Printing in progress");
                m_window.Visibility = m_window.IsVisible ? Visibility.Hidden : Visibility.Visible;
            }
        }

        /// <summary>
        /// Timeout method to the display timer.
        /// </summary>
        /// <param name="sender">Timer.</param>
        /// <param name="e">Argument.</param>
        private void M_timer_Tick(object sender, EventArgs e)
        {
            countdown();
        }

        /// <summary>
        /// Callback when main window is closing.
        /// </summary>
        /// <param name="sender">Window.</param>
        /// <param name="e">Argument.</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m_window != null)
            {
                m_window.Close();
            }
        } 

        /// <summary>
        /// Callback when window is loaded.
        /// </summary>
        /// <param name="sender">Window.</param>
        /// <param name="e">Argument.</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.LocationChanged += new EventHandler(MainWindow_LocationChanged);
            cameraControl.LayoutUpdated += new EventHandler(m_cameraControl_LayoutUpdated);
        }

        /// <summary>
        /// Callback when window location changed.
        /// </summary>
        /// <param name="sender">Window.</param>
        /// <param name="e">Argument.</param>
        void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            SyncOverlayPosition();
        }

        /// <summary>
        /// Callback when camera control layout is updated.
        /// </summary>
        /// <param name="sender">CameraControl object.</param>
        /// <param name="e">Argument.</param>
        void m_cameraControl_LayoutUpdated(object sender, EventArgs e)
        {
            SyncOverlayPosition();
        }

        /// <summary>
        /// Synchronized the main window and the overlay window.
        /// 
        /// Taken from http://stackoverflow.com/a/11749190.
        /// </summary>
        void SyncOverlayPosition()
        {
            System.Windows.Point hostTopLeft = cameraControl.PointToScreen(new System.Windows.Point(0, 0));

            m_window.Left = hostTopLeft.X;
            m_window.Top = hostTopLeft.Y;
            m_window.Width = this.ActualWidth;
            m_window.Height = this.ActualHeight;
        }

        /// <summary>
        /// Callback when pressing a key down.
        /// </summary>
        /// <param name="sender">Control object.</param>
        /// <param name="e">Event for key down.</param>
        private void CameraControl_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.A && e.Key <= Key.Z) || (e.Key >= Key.D0 && e.Key <= Key.D9))
            {
                m_cheatCode += new KeyConverter().ConvertToString(e.Key);
            }

            if (m_cheatCode == CHEATCODE_EXIT)
            {
                m_cheatCode = "";
                File.Delete(DUMMY_FILENAME);
                Close();
                return;
            }
            else if (m_cheatCode == CHEATCODE_RESET)
            {
                m_cheatCode = "";
                File.Delete(DUMMY_FILENAME);
                m_cpt = 5;
                m_img = 0;
                m_timer.Stop();
                return;
            }
            else if (m_cheatCode == CHEATCODE_WEBCAM_CHANGE)
            {
                m_cheatCode = "";
                MessageBox.Show("Switch webcam");

                m_cpt = 5;
                m_img = 0;
                m_timer.Stop();

                if(m_cameraChoice.Devices.Count == 1)
                {
                    return;
                }

                m_cameraIndex++;
                if(m_cameraIndex >= m_cameraChoice.Devices.Count)
                {
                    m_cameraIndex = 0;
                }

                ResolutionList resolutions = Camera.GetResolutionList(m_cameraChoice.Devices[m_cameraIndex].Mon);
                Resolution max = null;

                // use the maximum resolution
                foreach (Resolution res in resolutions)
                {
                    if(max == null)
                    {
                        max = res;
                        continue;
                    }

                    if(res.Width > max.Width)
                    {
                        max = res;
                    }
                }

                cameraControl.CameraControl.SetCamera(m_cameraChoice.Devices[m_cameraIndex].Mon, max);

                return;
            }
            else if (m_cheatCode == CHEATCODE_FULLSCREEN)
            {
                m_cheatCode = "";
                MessageBox.Show("Fullscreen");
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                this.Left = 0;
                this.Top = 0;
                this.Width = SystemParameters.VirtualScreenWidth;
                this.Height = SystemParameters.VirtualScreenHeight;
                return;
            }
            else if (m_cheatCode == CHEATCODE_NORMAL_SCREEN)
            {
                m_cheatCode = "";
                MessageBox.Show("Normal screen");
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowState = WindowState.Maximized;
                return;
            }
            else if (CHEATCODE_EXIT.StartsWith(m_cheatCode) ||
                CHEATCODE_RESET.StartsWith(m_cheatCode) ||
                CHEATCODE_WEBCAM_CHANGE.StartsWith(m_cheatCode) ||
                CHEATCODE_FULLSCREEN.StartsWith(m_cheatCode) ||
                CHEATCODE_NORMAL_SCREEN.StartsWith(m_cheatCode))
            {
                // OK keep adding the characters
            }
            else
            {
                m_cheatCode = "";
            }

            if (m_cpt == 5 && !File.Exists(DUMMY_FILENAME))
            {
                m_timer.Start();
            }
        }

        /// <summary>
        /// Launch the countdown and display it on the screen.
        /// </summary>
        private void countdown()
        {
            if(m_cpt == 0)
            {
                m_timer.Stop();

                Bitmap bmp = takePicture();

                // display captured image
                cameraControl.Visibility = Visibility.Hidden;
                img.Source = bitmapToImageSource(bmp);
                img.Visibility = Visibility.Visible;
                m_window.Visibility = Visibility.Hidden;

                // display it for 4 seconds
                DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
                timer.Tick += (sender, args) =>
                {
                    timer.Stop();

                    Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    string filename = "images\\" + unixTimestamp + ".jpg";

                    bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
                    
                    // stop timer and reset counter
                    m_cpt = 5;

                    // number of pictures taken for one cycle
                    m_img++;

                    if (m_img < MAX_IMAGES)
                    {
                        // start another shooting because we need X pictures
                        m_timer.Start();
                    }
                    else
                    {
                        // all pictures is taken so write the dummy file
                        m_img = 0;

                        try
                        {
                            System.IO.FileStream f = System.IO.File.OpenWrite(DUMMY_FILENAME);
                            f.WriteByte(0x01);
                            m_displayTimer.Start();
                        }
                        finally
                        {
                        }
                    }

                    img.Visibility = Visibility.Hidden;
                    cameraControl.Visibility = Visibility.Visible;
                };
                // one shot timer
                timer.Start();
            }
            else
            {
                m_window.Visibility = Visibility.Visible;
                m_window.setText(m_cpt.ToString());
                m_cpt--;
            }
        }

        /// <summary>
        /// Take the current picture and gets its bitmap representation.
        /// </summary>
        /// <returns>Image bitmap.</returns>
        private Bitmap takePicture()
        {
            Bitmap bmp = cameraControl.CameraControl.SnapshotSourceImage();

            return bmp;
        }
    }
}
