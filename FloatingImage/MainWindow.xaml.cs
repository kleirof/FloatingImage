using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Interop;
using System.Windows.Threading;

namespace FloatingImage
{
    public partial class MainWindow : Window
    {
        private bool isEditMode = true;

        private BitmapImage cachedMaximizeIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/maximize_icon.png", UriKind.RelativeOrAbsolute));
        private BitmapImage cachedRestoreIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/restore_icon.png", UriKind.RelativeOrAbsolute));

        private MenuItem maximizeRestoreMenuItem;

        internal int HotKeyId { get; private set; } = 1;
        internal string CurrentHotkey { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            this.Width = screenWidth / 2;
            this.Height = screenHeight / 2;

            this.Left = (screenWidth - this.Width) / 2;
            this.Top = (screenHeight - this.Height) / 2;

            this.Topmost = true;

            RenderOptions.SetBitmapScalingMode(ImageControl, BitmapScalingMode.HighQuality);

            this.PreviewMouseLeftButtonDown += MainWindow_PreviewMouseLeftButtonDown;

            ContextMenu contextMenu = new ContextMenu();

            MenuItem openImageMenuItem = new MenuItem { Header = "Open Image" };
            openImageMenuItem.Click += OpenImageMenuItem_Click;
            contextMenu.Items.Add(openImageMenuItem);

            MenuItem settingsMenuItem = new MenuItem { Header = "Settings" };
            settingsMenuItem.Click += SettingsMenuItem_Click;
            contextMenu.Items.Add(settingsMenuItem);

            MenuItem lockMenuItem = new MenuItem { Header = "Lock" };
            lockMenuItem.Click += LockMenuItem_Click;
            contextMenu.Items.Add(lockMenuItem);

            MenuItem maximizeRestoreMenuItem = new MenuItem { Header = "Maximize" };
            maximizeRestoreMenuItem.Click += MaximizeRestoreMenuItem_Click;
            contextMenu.Items.Add(maximizeRestoreMenuItem);
            this.maximizeRestoreMenuItem = maximizeRestoreMenuItem;

            MenuItem minimizeMenuItem = new MenuItem { Header = "Minimize" };
            minimizeMenuItem.Click += MinimizeMenuItem_Click;
            contextMenu.Items.Add(minimizeMenuItem);

            MenuItem closeMenuItem = new MenuItem { Header = "Close" };
            closeMenuItem.Click += CloseMenuItem_Click;
            contextMenu.Items.Add(closeMenuItem);

            ImageControl.ContextMenu = contextMenu;
            BackgroundRectangle.ContextMenu = contextMenu;

            ImageControl.Visibility = Visibility.Collapsed;

            this.Loaded += MainWindow_Loaded;
        }

        private void RegisterGlobalHotKey()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            try
            {
                if (!User32.RegisterHotKey(hwnd, HotKeyId, User32.MOD_SHIFT | User32.MOD_ALT, (int)KeyInterop.VirtualKeyFromKey(Key.Z)))
                    throw new ArgumentException("快捷键注册失败.");

                CurrentHotkey = "Shift+Alt+Z";
            }
            catch(Exception ex)
            {
                MessageBox.Show($"{ex.Message}");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HwndSource hwndSource = (HwndSource)HwndSource.FromVisual(this);
            if (hwndSource != null)
                hwndSource.AddHook(WndProc);

            RegisterGlobalHotKey();
            this.ResizeMode = ResizeMode.CanResizeWithGrip;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == User32.WM_HOTKEY)
            {
                if ((int)wParam == HotKeyId)
                {
                    ToggleEditMode();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void ToggleEditMode()
        {
            if (ImageControl.Source == null)
                return;

            isEditMode = !isEditMode;

            if (isEditMode)
            {
                DragButton.Visibility = Visibility.Visible;
                OpenButton.Visibility = Visibility.Visible;
                SettingsButton.Visibility = Visibility.Visible;
                LockButton.Visibility = Visibility.Visible;
                MinimizeButton.Visibility = Visibility.Visible;
                MaximizeRestoreButton.Visibility = Visibility.Visible;
                CloseButton.Visibility = Visibility.Visible;

                this.ResizeMode = ResizeMode.CanResizeWithGrip;
                SetWindowStyleForClickThrough(false);
            }
            else
            {
                DragButton.Visibility = Visibility.Collapsed;
                OpenButton.Visibility = Visibility.Collapsed;
                SettingsButton.Visibility = Visibility.Collapsed;
                LockButton.Visibility = Visibility.Collapsed;
                MinimizeButton.Visibility = Visibility.Collapsed;
                MaximizeRestoreButton.Visibility = Visibility.Collapsed;
                CloseButton.Visibility = Visibility.Collapsed;

                this.ResizeMode = ResizeMode.NoResize;
                SetWindowStyleForClickThrough(true);
            }
        }

        private void SetWindowStyleForClickThrough(bool makeClickThrough)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var style = (uint)User32.GetWindowLong(hwnd, User32.GWL_EXSTYLE);

            if (makeClickThrough)
                style |= (uint)User32.WS_EX_TRANSPARENT;
            else
                style &= ~(uint)User32.WS_EX_TRANSPARENT;

            User32.SetWindowLong(hwnd, User32.GWL_EXSTYLE, style);
        }

        private void MainWindow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isEditMode && !IsMouseOverNonDraggableButton(e))
                this.DragMove();
        }

        private bool IsMouseOverNonDraggableButton(MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(this);

            var openIconButtonBounds = OpenButton.TransformToAncestor(this).TransformBounds(new Rect(OpenButton.RenderSize));
            var settingsIconButtonBounds = SettingsButton.TransformToAncestor(this).TransformBounds(new Rect(SettingsButton.RenderSize));
            var lockIconButtonBounds = LockButton.TransformToAncestor(this).TransformBounds(new Rect(LockButton.RenderSize));
            var minimizeIconButtonBounds = MinimizeButton.TransformToAncestor(this).TransformBounds(new Rect(MinimizeButton.RenderSize));
            var maximizeRestoreIconButtonBounds = MaximizeRestoreButton.TransformToAncestor(this).TransformBounds(new Rect(MaximizeRestoreButton.RenderSize));
            var closeIconButtonBounds = CloseButton.TransformToAncestor(this).TransformBounds(new Rect(CloseButton.RenderSize));

            return openIconButtonBounds.Contains(pos) || settingsIconButtonBounds.Contains(pos) || lockIconButtonBounds.Contains(pos) || minimizeIconButtonBounds.Contains(pos) || maximizeRestoreIconButtonBounds.Contains(pos) || closeIconButtonBounds.Contains(pos);
        }

        private void OpenImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenImage();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            EnterSettings();
        }

        private void MaximizeRestoreMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MaximizeOrRestoreWindow();
        }

        private void LockMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleEditMode();
        }

        private void MinimizeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MinimizeWindow();
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CloseApplication();
        }

        protected override void OnClosed(EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            User32.UnregisterHotKey(hwnd, HotKeyId);
            base.OnClosed(e);
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenImage();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            EnterSettings();
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleEditMode();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeWindow();
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            MaximizeOrRestoreWindow();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseApplication();
        }

        private void SetButtonIcon(Button button, BitmapImage newIcon)
        {
            if (button.Content is Image image)
                image.Source = newIcon;
        }


        private void OpenImage()
        {
            if (!isEditMode)
                return;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "图像文件|*.png;*.jpg;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                BitmapImage newBitmapImage = new BitmapImage(new Uri(filePath));
                ImageControl.Source = newBitmapImage;

                ImageControl.Visibility = Visibility.Visible;
                BackgroundRectangle.Visibility = Visibility.Collapsed;
            }
        }

        private void EnterSettings()
        {
            if (!isEditMode)
                return;

            SettingsWindow settingsWindow = new SettingsWindow();

            settingsWindow.Owner = this;
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingsWindow.HotkeyTextBox.Text = CurrentHotkey;
            settingsWindow.ShowDialog();
        }

        private void MaximizeOrRestoreWindow()
        {
            if (!isEditMode)
                return;

            if (this.WindowState == WindowState.Maximized)
            {
                SetButtonIcon(MaximizeRestoreButton, cachedMaximizeIcon);
                maximizeRestoreMenuItem.Header = "Maximize";
                this.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
                this.WindowState = WindowState.Normal;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                SetButtonIcon(MaximizeRestoreButton, cachedRestoreIcon);
                maximizeRestoreMenuItem.Header = "Restore";
                this.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
                this.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeWindow()
        {
            if (!isEditMode)
                return;

            this.WindowState = WindowState.Minimized;
        }

        private void CloseApplication()
        {
            if (!isEditMode)
                return;

            this.Close();
        }
    }

    public static class User32
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WM_HOTKEY = 0x0312;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_ALT = 0x0001;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
