// ClipboardTyper.exe
// Global hotkey (Ctrl+Shift+V) that types the current clipboard after a delay.
// WinForms tray app; uses SendInput with KEYEVENTF_UNICODE for true keystrokes.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using Microsoft.Win32;
using System.Text;

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

        private const string VersionLabel = "v0.2";
        private const string Banner = @"██████╗ ██╗     ██╗██████╗ ██████╗  ██████╗  █████╗ ██████╗ ██████╗
██╔════╝██║     ██║██╔══██╗██╔══██╗██╔═══██╗██╔══██╗██╔══██╗██╔══██╗
██║     ██║     ██║██████╔╝██████╔╝██║   ██║███████║██████╔╝██║  ██║
██║     ██║     ██║██╔═══╝ ██╔══██╗██║   ██║██╔══██║██╔══██╗██║  ██║
╚██████╗███████╗██║██║     ██████╔╝╚██████╔╝██║  ██║██║  ██║██████╔╝
 ╚═════╝╚══════╝╚═╝╚═╝     ╚═════╝  ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝
      ████████╗██╗   ██╗██████╗ ███████╗██████╗
      ╚══██╔══╝╚██╗ ██╔╝██╔══██╗██╔════╝██╔══██╗
         ██║    ╚████╔╝ ██████╔╝█████╗  ██████╔╝
         ██║     ╚██╔╝  ██╔═══╝ ██╔══╝  ██╔══██╗
         ██║      ██║   ██║     ███████╗██║  ██║
         ╚═╝      ╚═╝   ╚═╝     ╚══════╝╚═╝  ╚═╝
         made by WAM-Software (c) 1997-2025";

        private const int MaxHistory = 10;
        private NotifyIcon _tray;
        private ContextMenuStrip _menu;
        private int _delayMs = 5000;
        private int _perCharDelayMs = 60; // slower typing per character
        private ToolStripMenuItem _startupItem;
        private readonly List<string> _history = new List<string>();

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
            AddHistoryEntry(text);
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
            _menu = new ContextMenuStrip();
            _menu.Opening += (sender, args) => RebuildMenu();
            RebuildMenu();

            _tray = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Visible = true,
                Text = Tooltip(),
                ContextMenuStrip = _menu
            };

            _tray.DoubleClick += (sender, args) => ShowBalloon(string.Format("Actief. Delay {0} ms. Hotkey Ctrl+Shift+V. Typen {1} ms/teken.", _delayMs, _perCharDelayMs));
            _tray.MouseClick += TrayMouseClick;
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
            RebuildMenu();
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
                RebuildMenu();
            }
            catch (Exception ex)
            {
                ShowBalloon("Kon opstartinstelling niet wijzigen: " + ex.Message);
            }
        }

        private void RebuildMenu()
        {
            if (_menu == null) return;

            _menu.Items.Clear();
            var header = _menu.Items.Add("History (last 10)");
            header.Enabled = false;
            if (_history.Count == 0)
            {
                _menu.Items.Add("   (empty)").Enabled = false;
            }
            else
            {
                for (int i = 0; i < _history.Count; i++)
                {
                    var label = string.Format("{0}. {1}", i + 1, _history[i]);
                    _menu.Items.Add(label).Enabled = false;
                }
            }

            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Type clipboard (Ctrl+Shift+V)").Enabled = false;
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Delay 5s", null, (sender, args) => SetDelay(5000));
            _menu.Items.Add("Delay 2s", null, (sender, args) => SetDelay(2000));
            _menu.Items.Add("Delay 0s", null, (sender, args) => SetDelay(0));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Type snelheid: Extra rustig (60ms)", null, (sender, args) => SetTypeSpeed(60));
            _menu.Items.Add("Type snelheid: Rustig (40ms)", null, (sender, args) => SetTypeSpeed(40));
            _menu.Items.Add("Type snelheid: Normaal (20ms)", null, (sender, args) => SetTypeSpeed(20));
            _menu.Items.Add("Type snelheid: Snel (10ms)", null, (sender, args) => SetTypeSpeed(10));
            _menu.Items.Add(new ToolStripSeparator());
            _startupItem = new ToolStripMenuItem("Start met Windows", null, (sender, args) => ToggleStartup());
            _startupItem.Checked = IsStartupEnabled();
            _menu.Items.Add(_startupItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Exit", null, (sender, args) => Close());
        }

        private void AddHistoryEntry(string text)
        {
            string cleaned = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            if (cleaned.Length > 80)
            {
                cleaned = cleaned.Substring(0, 77) + "...";
            }
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                cleaned = "(empty/whitespace)";
            }

            _history.Insert(0, cleaned);
            if (_history.Count > MaxHistory)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        private void TrayMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowInfoBox();
            }
        }

        private void ShowInfoBox()
        {
            using (var form = new Form())
            {
                form.Text = "Clipboard Typer";
                form.StartPosition = FormStartPosition.CenterScreen;
                form.ClientSize = new Size(1200, 750); // give banner room

                var bannerImage = RenderBannerImage();
                var picture = new PictureBox
                {
                    Image = bannerImage,
                    Dock = DockStyle.Top,
                    Height = bannerImage.Height + 10,
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    BackColor = Color.White
                };

                var infoLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10),
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    Text = BuildInfoText(),
                    AutoEllipsis = false
                };

                var close = new Button
                {
                    Text = "Close",
                    Dock = DockStyle.Bottom,
                    Height = 38
                };
                close.Click += (s, e) => form.Close();

                form.Controls.Add(infoLabel);
                form.Controls.Add(picture);
                form.Controls.Add(close);
                form.AcceptButton = close;

                form.FormClosed += (s, e) =>
                {
                    if (picture.Image != null)
                    {
                        picture.Image.Dispose();
                    }
                };

                form.ShowDialog();
            }
        }

        private Bitmap RenderBannerImage()
        {
            var lines = Banner.Split(new[] { '\n' }, StringSplitOptions.None);
            using (var font = new Font("Consolas", 11f, FontStyle.Regular))
            {
                var sampleSize = TextRenderer.MeasureText("W", font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                int charWidth = sampleSize.Width;
                int lineHeight = sampleSize.Height;
                int maxChars = lines.Max(l => l.Length);
                int width = Math.Max(800, charWidth * maxChars + 20);
                int height = Math.Max(300, lineHeight * lines.Length + 20);

                var bmp = new Bitmap(width, height);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        TextRenderer.DrawText(
                            g,
                            lines[i],
                            font,
                            new Point(10, 10 + i * lineHeight),
                            Color.Black,
                            TextFormatFlags.NoPadding | TextFormatFlags.NoClipping
                        );
                    }
                }
                return bmp;
            }
        }

        private string BuildInfoText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Version: " + VersionLabel);
            sb.AppendLine("Hotkey: Ctrl+Shift+V");
            sb.AppendLine("Delay: " + _delayMs + " ms");
            sb.AppendLine("Typing: " + _perCharDelayMs + " ms/char + 5 ms micro-pause");
            sb.AppendLine("Credits: WAM-Software (c) 1997-2025");
            sb.AppendLine("Repo: https://github.com/wmostert76/clipboard-typer");
            return sb.ToString();
        }
    }
}
