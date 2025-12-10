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
using System.Reflection;
using System.IO;

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
                form.Text = "Clipboard Typer Info";
                form.StartPosition = FormStartPosition.CenterScreen;
                form.Size = new Size(700, 850); // Increased height for more vertical space
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = Color.FromArgb(240, 240, 240); // Light grey background

                var bannerImage = LoadBannerImage();
                var picture = new PictureBox
                {
                    Image = bannerImage,
                    Dock = DockStyle.Top,
                    Height = 350, // Adjusted height for banner
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Black
                };

                // Main panel for all content below the picture
                var mainContentPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(30) // Increased padding
                };

                // Title Label
                var titleLabel = new Label
                {
                    Text = "CLIPBOARD TYPER " + VersionLabel,
                    Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 44, 52), // Dark text
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(0, 20, 0, 30) // Increased top and bottom margin
                };

                // Table for key-value pairs
                var tableLayoutPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    ColumnCount = 2,
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(15), // Increased padding
                    Margin = new Padding(0, 0, 0, 30) // Increased bottom margin
                };
                tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F)); // Key column
                tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Value column

                // Add rows for information
                AddInfoRow(tableLayoutPanel, "Hotkey:", "Ctrl + Shift + V", Color.Gray, FontStyle.Bold);
                AddInfoRow(tableLayoutPanel, "Current Delay:", string.Format("{0} ms", _delayMs), Color.DarkGray);
                AddInfoRow(tableLayoutPanel, "Typing Speed:", string.Format("{0} ms/char", _perCharDelayMs), Color.DarkGray);
                AddInfoRow(tableLayoutPanel, "Repo:", "https://github.com/wmostert76/clipboard-typer", Color.Gray);

                // Copyright Label
                var copyrightLabel = new Label
                {
                    Text = "© 2025 WAM-Software",
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = Color.Gray,
                    Dock = DockStyle.Bottom,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(0, 30, 0, 20) // Increased top and bottom margin
                };

                var closeButton = new Button
                {
                    Text = "Close",
                    Dock = DockStyle.Bottom,
                    Height = 45,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(40, 44, 52),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 0, 20) // Increased bottom margin
                };
                closeButton.FlatAppearance.BorderSize = 0;
                closeButton.Click += (s, e) => form.Close();

                // Add controls to main content panel
                mainContentPanel.Controls.Add(copyrightLabel); // Added first so it docks bottom correctly
                mainContentPanel.Controls.Add(tableLayoutPanel);
                mainContentPanel.Controls.Add(titleLabel);

                // Add controls to form
                form.Controls.Add(mainContentPanel);
                form.Controls.Add(closeButton); // Close button below main content panel
                form.Controls.Add(picture);
                form.AcceptButton = closeButton;

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

        private void AddInfoRow(TableLayoutPanel table, string key, string value, Color valueColor, FontStyle valueStyle = FontStyle.Regular)
        {
            var keyLabel = new Label
            {
                Text = key,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Padding = new Padding(8, 8, 5, 8) // Increased vertical padding
            };
            var valueLabel = new Label
            {
                Text = value,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f, valueStyle),
                ForeColor = valueColor,
                Padding = new Padding(5, 8, 8, 8) // Increased vertical padding
            };

            table.Controls.Add(keyLabel);
            table.Controls.Add(valueLabel);
        }

        private Image LoadBannerImage()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("banner.png"))
                {
                    if (stream != null)
                    {
                        return Image.FromStream(stream);
                    }
                }
            }
            catch { }
            
            // Fallback if resource not found
            var bmp = new Bitmap(800, 200);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.DarkBlue);
                g.DrawString("Clipboard Typer", new Font("Arial", 24), Brushes.White, 10, 80);
            }
            return bmp;
        }

        private string BuildInfoText()
        {
            // This method is no longer used for dynamic text assembly as content is now in individual labels.
            // Keeping for potential future use or if some fallback is needed.
            return "";
        }
    }
}
