/*
 * Copyright (c) 2017, Sebastien Vincent
 *
 * Distributed under the terms of the BSD 3-clause License.
 * See the LICENSE file for details.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace photomaton_wpf
{
    /// <summary>
    /// Interaction logic for TransparentWindow.xaml
    /// 
    /// This window is bordeless, with no style and does not appear in taskbar as well as alt-tab.
    /// Ideas is taken from http://stackoverflow.com/a/551847
    /// </summary>
    public partial class TransparentWindow : Window
    {
        [Flags]
        private enum ExtendedWindowStyles
        {
            WS_EX_TOOLWINDOW = 0x00000080,
        }

        private enum GetWindowLongFields
        {
            GWL_EXSTYLE = (-20),
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 SetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);
 
        /// <summary>
        /// Constructor.
        /// </summary>
        public TransparentWindow()
        {
            InitializeComponent();

            this.Loaded += TransparentWindow_Loaded;
        }

        /// <summary>
        /// Set text on the label.
        /// </summary>
        /// <param name="text"></param>
        public void setText(string text)
        {
            lbl.Content = text;
        }

        /// <summary>
        /// Callback when the window is loaded.
        /// </summary>
        /// <param name="sender">Window.</param>
        /// <param name="e">Argument.</param>
        private void TransparentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Use the following to avoid the window to be in the list of windows (in alt-tab for example).
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (Int32)exStyle);
        }
    }
}
