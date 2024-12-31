using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace FloatingImage
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void SettingsWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key pressedKey = e.Key == Key.System ? e.SystemKey : e.Key;

            string keyString = string.Empty;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                keyString += "Ctrl+";

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                keyString += "Shift+";

            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                keyString += "Alt+";

            if (string.IsNullOrEmpty(keyString) || IsSpecialKey(pressedKey))
            {
                e.Handled = true;
                return;
            }

            if (pressedKey != Key.LeftCtrl && pressedKey != Key.RightCtrl &&
                pressedKey != Key.LeftShift && pressedKey != Key.RightShift &&
                pressedKey != Key.LeftAlt && pressedKey != Key.RightAlt &&
                pressedKey != Key.System)
            {
                keyString += pressedKey.ToString();
            }

            HotkeyTextBox.Text = keyString;
            e.Handled = true;
        }

        private void SettingsWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None 
                && (HotkeyTextBox.Text.EndsWith("Ctrl+") || HotkeyTextBox.Text.EndsWith("Shift+") || HotkeyTextBox.Text.EndsWith("Alt+")))
                HotkeyTextBox.Clear();
        }


        private bool IsSpecialKey(Key key)
        {
            switch (key)
            {
                case Key.LWin:
                case Key.RWin:
                case Key.Apps:
                case Key.CapsLock:
                case Key.NumLock:
                case Key.Scroll:
                case Key.PrintScreen:
                    return true;
                default:
                    return false;
            }
        }

        private void SettingsWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
                e.Handled = true;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string hotkey = HotkeyTextBox.Text.Trim();

            if (string.IsNullOrEmpty(hotkey))
            {
                HotkeyTextBox.Text = (this.Owner as MainWindow).CurrentHotkey;
                MessageBox.Show("快捷键格式错误。");
                return;
            }

            try
            {
                if (hotkey != (this.Owner as MainWindow).CurrentHotkey)
                {
                    RegisterNewHotkey(hotkey);
                    (this.Owner as MainWindow).CurrentHotkey = hotkey;
                }
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                HotkeyTextBox.Text = (this.Owner as MainWindow).CurrentHotkey;
                MessageBox.Show($"{ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void RegisterNewHotkey(string hotkey)
        {
            string[] parts = hotkey.Split('+');
            uint fsModifiers = 0;
            string key = string.Empty;

            foreach (var part in parts)
            {
                switch (part.Trim().ToLower())
                {
                    case "ctrl":
                        fsModifiers |= User32.MOD_CONTROL;
                        break;
                    case "shift":
                        fsModifiers |= User32.MOD_SHIFT;
                        break;
                    case "alt":
                        fsModifiers |= User32.MOD_ALT;
                        break;
                    default:
                        key = part.Trim().ToUpper();
                        break;
                }
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("快捷键格式错误。");
            }

            int vk = (int)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), key));
            IntPtr hwnd = new WindowInteropHelper(Application.Current.MainWindow).Handle;

            if (!User32.RegisterHotKey(hwnd, (this.Owner as MainWindow).HotKeyId, fsModifiers, vk))
                throw new ArgumentException("快捷键注册失败。");
        }
    }
}
