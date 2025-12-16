using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsFocuser.Helpers;

namespace WindowsFocuser.Services
{
    public class TrayIconService : IDisposable
    {
        private const int TRAY_ID = 9001;
        private const int WM_TRAYICON = PInvoke.WM_TRAYICON;

        private IntPtr _hWnd;
        private IntPtr _oldWndProc;
        private PInvoke.WndProcDelegate _newWndProc;
        private bool _isSubclassed = false;

        // Menu IDs
        private const uint IDM_TOGGLE = 1001;
        private const uint IDM_SETTINGS = 1002;
        private const uint IDM_RELOAD = 1003;
        private const uint IDM_ABOUT = 1004;
        private const uint IDM_EXIT = 1005;

        public TrayIconService(IntPtr hWnd)
        {
            _hWnd = hWnd;
        }

        public void Initialize()
        {
            // Subclass
            if (!_isSubclassed)
            {
                _newWndProc = new PInvoke.WndProcDelegate(WndProc);
                _oldWndProc = PInvoke.SetWindowLongPtr(_hWnd, PInvoke.GWLP_WNDPROC, _newWndProc);
                _isSubclassed = true;
            }

            AddTrayIcon();
        }

        private void AddTrayIcon()
        {
            var nid = new PInvoke.NOTIFYICONDATA();
            nid.cbSize = Marshal.SizeOf(nid);
            nid.hWnd = _hWnd;
            nid.uID = TRAY_ID;
            nid.uFlags = PInvoke.NIF_ICON | PInvoke.NIF_MESSAGE | PInvoke.NIF_TIP;
            nid.uCallbackMessage = WM_TRAYICON;

            // Try to get app icon, fallback to system question mark or something
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            nid.hIcon = ExtractIcon(IntPtr.Zero, exePath, 0);
            if (nid.hIcon == IntPtr.Zero)
            {
                // Fallback
                nid.hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512); // IDI_APPLICATION
            }

            nid.szTip = "Windows Focuser";

            PInvoke.Shell_NotifyIcon(PInvoke.NIM_ADD, ref nid);
        }

        private void RemoveTrayIcon()
        {
            var nid = new PInvoke.NOTIFYICONDATA();
            nid.cbSize = Marshal.SizeOf(nid);
            nid.hWnd = _hWnd;
            nid.uID = TRAY_ID;
            PInvoke.Shell_NotifyIcon(PInvoke.NIM_DELETE, ref nid);
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAYICON)
            {
                if (lParam.ToInt32() == PInvoke.WM_RBUTTONUP)
                {
                    ShowContextMenu();
                }
                else if (lParam.ToInt32() == PInvoke.WM_LBUTTONUP)
                {
                    // Bring to front / Toggle
                    PInvoke.SetForegroundWindow(_hWnd);
                    // Maybe open settings window?
                }
            }
            else if (msg == 0x0111) // WM_COMMAND
            {
                uint commandId = (uint)wParam.ToInt32() & 0xFFFF;
                HandleCommand(commandId);
            }

            return PInvoke.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void ShowContextMenu()
        {
            IntPtr hMenu = PInvoke.CreatePopupMenu();

            bool isEnabled = App.Settings.IsEnabled;
            PInvoke.AppendMenu(hMenu, PInvoke.MF_STRING, IDM_TOGGLE, isEnabled ? "Disable" : "Enable");
            PInvoke.AppendMenu(hMenu, PInvoke.MF_SEPARATOR, 0, "");
            PInvoke.AppendMenu(hMenu, PInvoke.MF_STRING, IDM_SETTINGS, "Edit Settings (ini)");
            PInvoke.AppendMenu(hMenu, PInvoke.MF_STRING, IDM_RELOAD, "Reload Settings");
            PInvoke.AppendMenu(hMenu, PInvoke.MF_SEPARATOR, 0, "");
            PInvoke.AppendMenu(hMenu, PInvoke.MF_STRING, IDM_ABOUT, "About (GitHub)");
            PInvoke.AppendMenu(hMenu, PInvoke.MF_STRING, IDM_EXIT, "Exit");

            PInvoke.GetCursorPos(out var pt);
            PInvoke.SetForegroundWindow(_hWnd); // Required for menu to close on outside click
            PInvoke.TrackPopupMenu(hMenu, PInvoke.TPM_RIGHTBUTTON, pt.X, pt.Y, 0, _hWnd, IntPtr.Zero);
            PInvoke.DestroyMenu(hMenu);
        }

        private void HandleCommand(uint commandId)
        {
            switch (commandId)
            {
                case IDM_TOGGLE:
                    App.Settings.IsEnabled = !App.Settings.IsEnabled;
                    App.Settings.Save();
                    if (App.Settings.IsEnabled) App.FocusService.Start(); else App.FocusService.Stop();
                    break;
                case IDM_SETTINGS:
                    try { Process.Start(new ProcessStartInfo("notepad.exe", App.Settings.GetSettingsFilePath()) { UseShellExecute = true }); } catch {}
                    break;
                case IDM_RELOAD:
                    App.Settings.Load();
                    App.FocusService.UpdateSettings();
                    if (App.Settings.IsEnabled) App.FocusService.Start(); else App.FocusService.Stop();
                    break;
                case IDM_ABOUT:
                    try { Process.Start(new ProcessStartInfo("https://github.com/") { UseShellExecute = true }); } catch {}
                    break;
                case IDM_EXIT:
                    App.Current.Exit();
                    break;
            }
        }

        public void Dispose()
        {
            RemoveTrayIcon();
            if (_isSubclassed && _oldWndProc != IntPtr.Zero)
            {
                PInvoke.SetWindowLongPtr(_hWnd, PInvoke.GWLP_WNDPROC, _oldWndProc);
                _oldWndProc = IntPtr.Zero;
                _isSubclassed = false;
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
    }
}
