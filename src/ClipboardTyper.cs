// ClipboardTyper.exe
// Global hotkey (Ctrl+Alt+V) that types the current clipboard after a delay.
// WinForms tray app; uses SendInput with KEYEVENTF_UNICODE for true keystrokes.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Text;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using Timer = System.Windows.Forms.Timer;

namespace ClipboardTyper
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "ClipboardTyper_SingleInstance_Mutex", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Clipboard Typer draait al. Kijk in het systeemvak (bij de klok).", "Clipboard Typer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp());
            }
        }
    }

    internal sealed class TrayApp : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private const string VersionLabel = "v1.0";
        private NotifyIcon _tray;
        private ContextMenuStrip _menu;
        private Form _infoForm;
        private int _delayMs = 5000;
        private int _perCharDelayMs = 60; // slower typing per character
        private ToolStripMenuItem _startupItem;
        private uint _hotkeyModifiers = MOD_CONTROL | MOD_ALT;
        private int _hotkeyKey = (int)Keys.V;

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
            LoadHotkeyFromRegistry();
            PromoteOwnTrayIcon();
            BuildTray();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RegisterHotkeyCurrent();
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

            _tray.DoubleClick += (sender, args) => ShowBalloon(string.Format("Actief. Delay {0} ms. Hotkey {1}. Typen {2} ms/teken.", _delayMs, HotkeyLabel(), _perCharDelayMs));
            _tray.MouseClick += TrayMouseClick;

            // Show a balloon tip on startup to draw attention to the tray icon
            ShowBalloon("Clipboard Typer is nu actief in het systeemvak.");
            RefreshTrayVisibility();
            EnsureTrayVisibleSoon();
        }

        private void SetDelay(int ms)
        {
            _delayMs = ms;
            _tray.Text = Tooltip();
            ShowBalloon(string.Format("Delay ingesteld op {0} ms", _delayMs));
            RebuildMenu();
        }

        private string Tooltip()
        {
            return string.Format("Clipboard Typer - {0} - delay {1} ms - {2} ms/teken", HotkeyLabel(), _delayMs, _perCharDelayMs);
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

        private string HotkeyLabel()
        {
            return FormatHotkey(_hotkeyModifiers, (Keys)_hotkeyKey);
        }

        private static string FormatHotkey(uint modifiers, Keys key)
        {
            var parts = new List<string>();
            if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
            if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
            if ((modifiers & MOD_WIN) != 0) parts.Add("Win");
            if (key != Keys.None)
            {
                parts.Add(key.ToString());
            }
            return string.Join(" + ", parts);
        }

        private static bool IsValidHotkey(uint modifiers, int key)
        {
            if (modifiers == 0) return false;
            var k = (Keys)key;
            if (k == Keys.ControlKey || k == Keys.Menu || k == Keys.ShiftKey || k == Keys.LWin || k == Keys.RWin) return false;
            return Enum.IsDefined(typeof(Keys), k);
        }

        private void RegisterHotkeyCurrent()
        {
            if (!IsValidHotkey(_hotkeyModifiers, _hotkeyKey) || !RegisterHotKey(Handle, HOTKEY_ID, _hotkeyModifiers, _hotkeyKey))
            {
                _hotkeyModifiers = MOD_CONTROL | MOD_ALT;
                _hotkeyKey = (int)Keys.V;
                RegisterHotKey(Handle, HOTKEY_ID, _hotkeyModifiers, _hotkeyKey);
            }
        }

        private void ApplyHotkey(uint modifiers, int key)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            if (!IsValidHotkey(modifiers, key) || !RegisterHotKey(Handle, HOTKEY_ID, modifiers, key))
            {
                ShowBalloon("Kon hotkey niet registreren. Kies een andere combinatie.");
                RegisterHotkeyCurrent();
                return;
            }

            _hotkeyModifiers = modifiers;
            _hotkeyKey = key;
            SaveHotkeyToRegistry();
            _tray.Text = Tooltip();
            RebuildMenu();
            ShowBalloon("Hotkey ingesteld op " + HotkeyLabel());
        }

        private void LoadHotkeyFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\ClipboardTyper", false))
                {
                    if (key == null) return;
                    var modsObj = key.GetValue("HotkeyModifiers");
                    var keyObj = key.GetValue("HotkeyKey");
                    if (modsObj != null && keyObj != null)
                    {
                        var mods = (int)modsObj;
                        var hotkey = (int)keyObj;
                        if (IsValidHotkey((uint)mods, hotkey))
                        {
                            _hotkeyModifiers = (uint)mods;
                            _hotkeyKey = hotkey;
                        }
                    }
                }
            }
            catch
            {
                // ignore and fall back to defaults
            }
        }

        private void SaveHotkeyToRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\ClipboardTyper"))
                {
                    if (key == null) return;
                    key.SetValue("HotkeyModifiers", (int)_hotkeyModifiers, RegistryValueKind.DWord);
                    key.SetValue("HotkeyKey", _hotkeyKey, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                ShowBalloon("Kon hotkey niet opslaan in HKCU: " + ex.Message);
            }
        }

        private void ChangeHotkey()
        {
            using (var form = new Form())
            {
                form.Text = "Hotkey instellen";
                form.StartPosition = FormStartPosition.CenterScreen;
                form.Size = new Size(360, 180);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.TopMost = true;

                var info = new Label
                {
                    Text = "Druk op de gewenste toetscombinatie (minimaal 1 modifier).",
                    Dock = DockStyle.Top,
                    AutoSize = false,
                    Height = 40,
                    Padding = new Padding(10, 10, 10, 0)
                };

                var capture = new TextBox
                {
                    Dock = DockStyle.Top,
                    ReadOnly = true,
                    TextAlign = HorizontalAlignment.Center,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    Margin = new Padding(12),
                    Height = 32
                };

                var panelButtons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Height = 46,
                    Padding = new Padding(8)
                };

                var ok = new Button { Text = "Opslaan", DialogResult = DialogResult.OK, Enabled = false, AutoSize = true, Padding = new Padding(10, 6, 10, 6) };
                var cancel = new Button { Text = "Annuleren", DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(10, 6, 10, 6) };
                panelButtons.Controls.Add(ok);
                panelButtons.Controls.Add(cancel);

                Keys selectedKey = Keys.None;
                uint selectedMods = 0;

                capture.KeyDown += (s, e) =>
                {
                    e.SuppressKeyPress = true;
                    selectedMods = 0;
                    if (e.Control) selectedMods |= MOD_CONTROL;
                    if (e.Alt) selectedMods |= MOD_ALT;
                    if (e.Shift) selectedMods |= MOD_SHIFT;
                    if (e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin) selectedMods |= MOD_WIN;
                    selectedKey = e.KeyCode;

                    if (selectedKey == Keys.ControlKey || selectedKey == Keys.Menu || selectedKey == Keys.ShiftKey || selectedKey == Keys.LWin || selectedKey == Keys.RWin)
                    {
                        capture.Text = "Kies ook een toets";
                        ok.Enabled = false;
                        return;
                    }

                    capture.Text = FormatHotkey(selectedMods, selectedKey);
                    ok.Enabled = selectedMods != 0 && selectedKey != Keys.None;
                };

                capture.KeyUp += (s, e) => e.SuppressKeyPress = true;

                form.Controls.Add(panelButtons);
                form.Controls.Add(capture);
                form.Controls.Add(info);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                capture.Focus();
                if (form.ShowDialog() == DialogResult.OK && ok.Enabled)
                {
                    ApplyHotkey(selectedMods, (int)selectedKey);
                }
            }
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
                _startupItem.Text = ActiveLabel("Start met Windows", enable);
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
            _menu.Items.Add(string.Format("Type clipboard ({0})", HotkeyLabel())).Enabled = false;
            _menu.Items.Add("Hotkey wijzigen...", null, (sender, args) => ChangeHotkey());
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(ActiveLabel("Delay 5s", _delayMs == 5000), null, (sender, args) => SetDelay(5000));
            _menu.Items.Add(ActiveLabel("Delay 2s", _delayMs == 2000), null, (sender, args) => SetDelay(2000));
            _menu.Items.Add(ActiveLabel("Delay 0s", _delayMs == 0), null, (sender, args) => SetDelay(0));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(ActiveLabel("Type snelheid: Extra rustig (60ms)", _perCharDelayMs == 60), null, (sender, args) => SetTypeSpeed(60));
            _menu.Items.Add(ActiveLabel("Type snelheid: Rustig (40ms)", _perCharDelayMs == 40), null, (sender, args) => SetTypeSpeed(40));
            _menu.Items.Add(ActiveLabel("Type snelheid: Normaal (20ms)", _perCharDelayMs == 20), null, (sender, args) => SetTypeSpeed(20));
            _menu.Items.Add(ActiveLabel("Type snelheid: Snel (10ms)", _perCharDelayMs == 10), null, (sender, args) => SetTypeSpeed(10));
            _menu.Items.Add(new ToolStripSeparator());
            _startupItem = new ToolStripMenuItem(ActiveLabel("Start met Windows", IsStartupEnabled()), null, (sender, args) => ToggleStartup());
            _startupItem.Checked = IsStartupEnabled();
            _menu.Items.Add(_startupItem);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add("Exit", null, (sender, args) => Close());
        }

        private void EnsureTrayVisibleSoon()
        {
            EnsureTrayVisible();
            var ensureTimer = new Timer { Interval = 2000 };
            ensureTimer.Tick += (s, e) =>
            {
                EnsureTrayVisible();
                ensureTimer.Stop();
                ensureTimer.Dispose();
            };
            ensureTimer.Start();
        }

        private void EnsureTrayVisible()
        {
            RefreshTrayVisibility();
        }

        private void RefreshTrayVisibility()
        {
            if (_tray == null) return;
            PromoteOwnTrayIcon();
            _tray.Visible = false;
            _tray.Visible = true;
        }

        private void PromoteOwnTrayIcon()
        {
            try
            {
                using (var root = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", true))
                {
                    if (root == null) return;
                    foreach (var name in root.GetSubKeyNames())
                    {
                        using (var sub = root.OpenSubKey(name, true))
                        {
                            if (sub == null) continue;
                            var exe = sub.GetValue("ExecutablePath") as string;
                            if (string.IsNullOrEmpty(exe)) continue;
                            if (exe.EndsWith("ClipboardTyper.exe", StringComparison.OrdinalIgnoreCase))
                            {
                                sub.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore if registry access is denied or unavailable.
            }
        }

        private static string ActiveLabel(string label, bool isActive)
        {
            return isActive ? "* " + label : label;
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
            if (_infoForm != null && !_infoForm.IsDisposed)
            {
                if (_infoForm.WindowState == FormWindowState.Minimized)
                {
                    _infoForm.WindowState = FormWindowState.Normal;
                }
                _infoForm.Activate();
                return;
            }

            _infoForm = new Form();
            var form = _infoForm;

            form.Text = "Clipboard Typer Info";
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Size = new Size(720, 880);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.BackColor = Color.FromArgb(245, 247, 250);
            form.AutoScaleMode = AutoScaleMode.Dpi;

            var bannerImage = LoadBannerImage();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = form.BackColor
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 280f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));

            var picture = new PictureBox
            {
                Image = bannerImage,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                Margin = new Padding(0)
            };
            root.Controls.Add(picture, 0, 0);

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = form.BackColor,
                Padding = new Padding(18, 14, 18, 10)
            };

            var card = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = Color.White,
                Padding = new Padding(18, 16, 18, 16),
                Margin = new Padding(0)
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            AddRow(card, new Label
            {
                Text = "CLIPBOARD TYPER " + VersionLabel,
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 32, 32),
                AutoSize = true,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 4)
            });

            AddRow(card, new Label
            {
                Text = "Types your clipboard with a configurable delay and pace.",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 14)
            });

            var tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(4),
                Margin = new Padding(0, 0, 0, 12)
            };
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));

            AddInfoRow(tableLayoutPanel, "Hotkey", HotkeyLabel(), Color.FromArgb(32, 32, 32), FontStyle.Bold);
            AddInfoRow(tableLayoutPanel, "Current delay", string.Format("{0} ms", _delayMs), Color.FromArgb(32, 32, 32));
            AddInfoRow(tableLayoutPanel, "Typing speed", string.Format("{0} ms/char", _perCharDelayMs), Color.FromArgb(32, 32, 32));
            AddInfoRow(tableLayoutPanel, "Start with Windows", IsStartupEnabled() ? "Enabled" : "Disabled", Color.FromArgb(32, 32, 32));

            AddRow(card, tableLayoutPanel);

            AddRow(card, CreateSectionLabel("Repo & support"));
            AddRow(card, CreateLinkLabel("https://github.com/wmostert76/clipboard-typer", "https://github.com/wmostert76/clipboard-typer"));

            AddRow(card, CreateSectionLabel("Quick tips"));
            AddRow(card, CreateBulletLabel("Left-click the tray icon to reopen this info box."));
            AddRow(card, CreateBulletLabel("Right-click the tray icon for delay and typing-speed presets."));
            AddRow(card, CreateBulletLabel("Use \"Start met Windows\" if you want it always ready after login."));
            AddRow(card, CreateBulletLabel("Keystrokes are sent as Unicode, so emoji and accents work where paste is blocked."));

            AddRow(card, CreateSectionLabel("About"));
            AddRow(card, new Label
            {
                Text = "Made by WAM-Software (c) since 1997",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 70, 70),
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 0)
            });

            var cardHost = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = form.BackColor,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardHost.Controls.Add(card);
            scrollPanel.Controls.Add(cardHost);

            root.Controls.Add(scrollPanel, 0, 1);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(38, 43, 51)
            };

            var closeButton = new Button
            {
                Text = "Close",
                AutoSize = false,
                Width = 130,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(64, 132, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => form.Close();

            bottomPanel.Controls.Add(closeButton);
            bottomPanel.Resize += (s, e) =>
            {
                closeButton.Left = (bottomPanel.ClientSize.Width - closeButton.Width) / 2;
                closeButton.Top = (bottomPanel.ClientSize.Height - closeButton.Height) / 2;
            };
            bottomPanel.PerformLayout();
            root.Controls.Add(bottomPanel, 0, 2);

            form.Controls.Add(root);
            form.AcceptButton = closeButton;

            form.FormClosed += (s, e) =>
            {
                if (picture.Image != null)
                {
                    picture.Image.Dispose();
                }
                _infoForm = null;
            };

            form.Show();
        }

        private void AddInfoRow(TableLayoutPanel table, string key, string value, Color valueColor, FontStyle valueStyle = FontStyle.Regular)
        {
            var keyLabel = new Label
            {
                Text = key,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.Black,
                Padding = new Padding(6, 6, 6, 6)
            };
            var valueLabel = new Label
            {
                Text = value,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f, valueStyle),
                ForeColor = valueColor,
                Padding = new Padding(6, 6, 6, 6)
            };

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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

        private void AddRow(TableLayoutPanel layout, Control control)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(control);
        }

        private Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 6)
            };
        }

        private Label CreateBulletLabel(string text)
        {
            return new Label
            {
                Text = "• " + text,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(55, 55, 55),
                AutoSize = true,
                MaximumSize = new Size(620, 0),
                Margin = new Padding(0, 2, 0, 2)
            };
        }

        private LinkLabel CreateLinkLabel(string text, string url)
        {
            var link = new LinkLabel
            {
                Text = text,
                AutoSize = true,
                LinkColor = Color.FromArgb(33, 150, 243),
                ActiveLinkColor = Color.FromArgb(255, 87, 34),
                VisitedLinkColor = Color.FromArgb(106, 27, 154),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Margin = new Padding(0, 0, 0, 8)
            };

            link.Links.Add(0, text.Length, url);
            link.LinkClicked += (s, e) =>
            {
                try
                {
                    var target = e.Link.LinkData as string;
                    if (!string.IsNullOrWhiteSpace(target))
                    {
                        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    ShowBalloon("Kon link niet openen: " + ex.Message);
                }
            };

            return link;
        }
    }
}
