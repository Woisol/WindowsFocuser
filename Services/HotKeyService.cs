using System;
using System.Runtime.InteropServices;
using WindowsFocuser.Helpers;

namespace WindowsFocuser.Services
{
    public class HotKeyService : IDisposable
    {
        private const int HOTKEY_ID = 9000;
        private IntPtr _hWnd;
        private IntPtr _oldWndProc;
        private PInvoke.WndProcDelegate _newWndProc;
        private SettingsService _settings;
        private Action _onHotKeyTriggered;
        private bool _isSubclassed = false;

        public HotKeyService(IntPtr hWnd, SettingsService settings, Action onHotKeyTriggered)
        {
            _hWnd = hWnd;
            _settings = settings;
            _onHotKeyTriggered = onHotKeyTriggered;
        }

        public void Register()
        {
            if (!_isSubclassed)
            {
                _newWndProc = new PInvoke.WndProcDelegate(WndProc);
                _oldWndProc = PInvoke.SetWindowLongPtr(_hWnd, PInvoke.GWLP_WNDPROC, _newWndProc);
                _isSubclassed = true;
            }

            RegisterHotKeyInternal();
        }

        public void UpdateHotKey()
        {
            PInvoke.UnregisterHotKey(_hWnd, HOTKEY_ID);
            RegisterHotKeyInternal();
        }

        private void RegisterHotKeyInternal()
        {
            bool success = PInvoke.RegisterHotKey(_hWnd, HOTKEY_ID, _settings.HotKeyModifiers, _settings.HotKeyKey);
            if (!success)
            {
                System.Diagnostics.Debug.WriteLine("Failed to register hotkey");
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == PInvoke.WM_HOTKEY)
            {
                if (wParam.ToInt32() == HOTKEY_ID)
                {
                    _onHotKeyTriggered?.Invoke();
                }
            }

            return PInvoke.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            PInvoke.UnregisterHotKey(_hWnd, HOTKEY_ID);
            if (_isSubclassed && _oldWndProc != IntPtr.Zero)
            {
                PInvoke.SetWindowLongPtr(_hWnd, PInvoke.GWLP_WNDPROC, _oldWndProc);
                _oldWndProc = IntPtr.Zero;
                _isSubclassed = false;
            }
        }
    }
}
