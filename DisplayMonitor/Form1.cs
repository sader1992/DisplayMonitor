using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DisplayMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        struct POINT
        {
            public int x;
            public int y;
        }

        const Int32 CURSOR_SHOWING = 0x00000001;

        Screen cp_screen = null;
        string WindowName = "";
        bool ScreenView = true;

        private void Form1_Load(object sender, EventArgs e)
        {
            menuStrip1.BackColor = Color.Transparent;
            menuStrip1.Dock = DockStyle.Top;
            menuStrip1.BringToFront();

            menuStrip1.Parent = pictureBox1;
            menuStrip1.BackColor = Color.Transparent;

            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.BorderStyle = BorderStyle.Fixed3D;

            timer1.Enabled = true;
            cp_screen = Screen.AllScreens[Screen.AllScreens.Length - 1];
            notifyIcon1.Visible = false;
        }

        private void CaptureScreen()
        {
            try
            {
                using (Bitmap captureBitmap = new Bitmap(cp_screen.Bounds.Width, cp_screen.Bounds.Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics captureGraphics = Graphics.FromImage(captureBitmap))
                    {
                        captureGraphics.CopyFromScreen(cp_screen.Bounds.X, cp_screen.Bounds.Y, 0, 0, cp_screen.Bounds.Size, CopyPixelOperation.SourceCopy);
                        

                        CURSORINFO cursorInfo = new CURSORINFO
                        {
                            cbSize = Marshal.SizeOf(typeof(CURSORINFO))
                        };

                        if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
                        {
                            int cursorX = cursorInfo.ptScreenPos.x - cp_screen.Bounds.X - 0;
                            int cursorY = cursorInfo.ptScreenPos.y - cp_screen.Bounds.Y - 0;
                            IntPtr hdc = captureGraphics.GetHdc();
                            DrawIcon(hdc, cursorX, cursorY, cursorInfo.hCursor);
                            captureGraphics.ReleaseHdc(hdc);
                            DestroyIcon(cursorInfo.hCursor);
                        }
                    }

                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = new Bitmap(captureBitmap, pictureBox1.Size);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CaptureWindow(string windowName)
        {
            IntPtr hWnd = FindWindow(null, windowName);
            if (hWnd != IntPtr.Zero)
            {
                if (GetWindowRect(hWnd, out RECT rect))
                {
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;

                    try
                    {
                        using (Bitmap captureBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics captureGraphics = Graphics.FromImage(captureBitmap))
                            {
                                captureGraphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                                CURSORINFO cursorInfo = new CURSORINFO
                                {
                                    cbSize = Marshal.SizeOf(typeof(CURSORINFO))
                                };

                                if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
                                {
                                    int cursorX = cursorInfo.ptScreenPos.x - rect.Left - 0;
                                    int cursorY = cursorInfo.ptScreenPos.y - rect.Top - 0;
                                    IntPtr hdc = captureGraphics.GetHdc();
                                    DrawIcon(hdc, cursorX, cursorY, cursorInfo.hCursor);
                                    captureGraphics.ReleaseHdc(hdc);
                                    DestroyIcon(cursorInfo.hCursor);
                                }
                                
                            }
                            pictureBox1.Image?.Dispose();
                            pictureBox1.Image = new Bitmap(captureBitmap, pictureBox1.Size);
                        }
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Unable to get window bounds.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Window not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized || !this.Visible)
            {
                return;
            }
            if (ScreenView)
            {
                if (cp_screen != null)
                    CaptureScreen();
            }
            else
            {
                if (WindowName != "")
                    CaptureWindow(WindowName);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Normal;
            timer1.Enabled = true;
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                timer1.Enabled = false;
                this.ShowInTaskbar = false;
            }

            if (this.WindowState == FormWindowState.Normal)
            {
                this.ShowInTaskbar = true;
            }
        }

        private void minimizeToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            notifyIcon1.BalloonTipTitle = "My Screen";
            this.ShowInTaskbar = false;
            notifyIcon1.Visible = true;
            this.Hide();
        }

        private void windowsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            windowsToolStripMenuItem.DropDownItems.Clear();
            EnumWindows(new EnumWindowsProc((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) > 0)
                {
                    RECT rect;
                    GetWindowRect(hWnd, out rect);
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    if (width > 50 && height > 50)
                    {
                        StringBuilder builder = new StringBuilder(GetWindowTextLength(hWnd) + 1);
                        GetWindowText(hWnd, builder, builder.Capacity);
                        string windowTitle = builder.ToString();
                        if (!string.IsNullOrWhiteSpace(windowTitle) && windowTitle != "Default IME" && windowTitle != "MSCTFIME UI")
                        {
                            ToolStripMenuItem windowItem = new ToolStripMenuItem
                            {
                                Text = windowTitle,
                                Tag = hWnd
                            };
                            windowItem.Click += WindowItem_Click;
                            windowsToolStripMenuItem.DropDownItems.Add(windowItem);
                        }
                    }
                }
                return true;
            }), IntPtr.Zero);
        }

        private void WindowItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
            IntPtr hWnd = (IntPtr)clickedItem.Tag;

            ScreenView = false;
            WindowName = clickedItem.Text;
        }

        private void screensToolStripMenuItem_Click(object sender, EventArgs e)
        {
            screensToolStripMenuItem.DropDownItems.Clear();
            foreach (Screen screen in Screen.AllScreens)
            {
                ToolStripMenuItem screenItem = new ToolStripMenuItem
                {
                    Text = $"Screen {screen.DeviceName.Replace("\\\\.\\DISPLAY", "")}: {screen.Bounds.Width}x{screen.Bounds.Height}"
                };
                screenItem.Click += ScreenItem_Click;
                screensToolStripMenuItem.DropDownItems.Add(screenItem);
            }
        }

        private void ScreenItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem)sender;
            int ndx = GetScreenIndex(clickedItem.Text);
            if (Screen.AllScreens.Length > ndx && ndx != -1)
            {
                cp_screen = Screen.AllScreens[ndx];
                ScreenView = true;
            }
            else
            {
                MessageBox.Show("Display " + ndx + " Is not connected.");
            }
        }

        private int GetScreenIndex(string screenName)
        {
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                string Text = $"Screen {Screen.AllScreens[i].DeviceName.Replace("\\\\.\\DISPLAY", "")}: {Screen.AllScreens[i].Bounds.Width}x{Screen.AllScreens[i].Bounds.Height}";
                if (Text == screenName)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
