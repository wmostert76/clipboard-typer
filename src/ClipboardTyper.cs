// ClipboardTyper.exe
// Global hotkey (Ctrl+Shift+V) that types the current clipboard after a delay.
// WinForms tray app; uses SendInput with KEYEVENTF_UNICODE for true keystrokes.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using Microsoft.Win32;

namespace ClipboardTyper
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }

    internal sealed class TrayApp : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        private NotifyIcon _tray;
        private int _delayMs = 5000;
        private int _perCharDelayMs = 60; // slower typing per character
        private ToolStripMenuItem _startupItem;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        public TrayApp()
        {
            Visible = false;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            BuildTray();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (int)Keys.V);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            _tray.Visible = false;
            _tray.Dispose();
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
#pragma warning disable 4014
                HandleHotkeyAsync();
#pragma warning restore 4014
            }
            base.WndProc(ref m);
        }

        private async Task HandleHotkeyAsync()
        {
            string text = string.Empty;
            try
            {
                text = Clipboard.GetText(TextDataFormat.UnicodeText);
            }
            catch
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            ShowBalloon(string.Format("Typen na {0:0.#}s...", _delayMs / 1000.0));
            await Task.Delay(_delayMs);
            TypeUnicode(text);
        }

        private void TypeUnicode(string text)
        {
            foreach (char ch in text)
            {
                var inputs = new INPUT[2];

                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].U.ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                inputs[1].type = INPUT_KEYBOARD;
                inputs[1].U.ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                Thread.Sleep(_perCharDelayMs);
                // Extra tiny pause helps reliability on some systems
                Thread.Sleep(5);
            }
        }

        private void BuildTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Type clipboard (Ctrl+Shift+V)").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Delay 5s", null, (sender, args) => SetDelay(5000));
            menu.Items.Add("Delay 2s", null, (sender, args) => SetDelay(2000));
            menu.Items.Add("Delay 0s", null, (sender, args) => SetDelay(0));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Type snelheid: Extra rustig (60ms)", null, (sender, args) => SetTypeSpeed(60));
            menu.Items.Add("Type snelheid: Rustig (40ms)", null, (sender, args) => SetTypeSpeed(40));
            menu.Items.Add("Type snelheid: Normaal (20ms)", null, (sender, args) => SetTypeSpeed(20));
            menu.Items.Add("Type snelheid: Snel (10ms)", null, (sender, args) => SetTypeSpeed(10));
            menu.Items.Add(new ToolStripSeparator());
            _startupItem = new ToolStripMenuItem("Start met Windows", null, (sender, args) => ToggleStartup());
            _startupItem.Checked = IsStartupEnabled();
            menu.Items.Add(_startupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (sender, args) => Close());

            _tray = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = Tooltip(),
                ContextMenuStrip = menu
            };

            _tray.DoubleClick += (sender, args) => ShowBalloon(string.Format("Actief. Delay {0} ms. Hotkey Ctrl+Shift+V. Typen {1} ms/teken.", _delayMs, _perCharDelayMs));
        }

        private void SetDelay(int ms)
        {
            _delayMs = ms;
            _tray.Text = Tooltip();
            ShowBalloon(string.Format("Delay ingesteld op {0} ms", _delayMs));
        }

        private string Tooltip()
        {
            return string.Format("Clipboard Typer - Ctrl+Shift+V - delay {0} ms - {1} ms/teken", _delayMs, _perCharDelayMs);
        }

        private void ShowBalloon(string message)
        {
            _tray.BalloonTipTitle = "Clipboard Typer";
            _tray.BalloonTipText = message;
            _tray.ShowBalloonTip(2000);
        }

        private void SetTypeSpeed(int msPerChar)
        {
            _perCharDelayMs = msPerChar;
            _tray.Text = Tooltip();
            ShowBalloon(string.Format("Typesnelheid ingesteld op {0} ms per teken", _perCharDelayMs));
        }

        private bool IsStartupEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                if (key == null) return false;
                var val = key.GetValue("ClipboardTyper") as string;
                if (string.IsNullOrEmpty(val)) return false;
                // normalize path with quotes
                string exePath = "\"" + Application.ExecutablePath + "\"";
                return string.Equals(val, exePath, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void ToggleStartup()
        {
            bool enable = !_startupItem.Checked;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            key.SetValue("ClipboardTyper", "\"" + Application.ExecutablePath + "\"");
                        }
                        else
                        {
                            key.DeleteValue("ClipboardTyper", false);
                        }
                    }
                }
                _startupItem.Checked = enable;
                ShowBalloon(enable ? "Opstarten met Windows: ingeschakeld" : "Opstarten met Windows: uit");
            }
            catch (Exception ex)
            {
                ShowBalloon("Kon opstartinstelling niet wijzigen: " + ex.Message);
            }
        }
    }
}
