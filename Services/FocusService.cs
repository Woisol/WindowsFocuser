using System;
using System.Diagnostics;
using WindowsFocuser.Helpers;

namespace WindowsFocuser.Services
{
    public class FocusService : IDisposable
    {
        private OverlayWindow _overlayWindow;
        private SettingsService _settingsService;
        private PInvoke.WinEventDelegate _winEventDelegate;
        private IntPtr _hook;

        public FocusService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public void Start()
        {
            if (_overlayWindow == null)
            {
                _overlayWindow = new OverlayWindow();
                // Start invisible to avoid flash
                _overlayWindow.SetOpacity(0);
                _overlayWindow.Activate();
            }

            UpdateOverlayBounds();

            if (_hook == IntPtr.Zero)
            {
                _winEventDelegate = new PInvoke.WinEventDelegate(WinEventProc);
                _hook = PInvoke.SetWinEventHook(
                    PInvoke.EVENT_SYSTEM_FOREGROUND,
                    PInvoke.EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0,
                    0,
                    PInvoke.WINEVENT_OUTOFCONTEXT);
            }

            // Trigger once for current window
            HandleFocusChange(PInvoke.GetForegroundWindow());

            // Restore opacity
            _overlayWindow.SetOpacity(_settingsService.DimOpacity);
        }

        public void Stop()
        {
            if (_hook != IntPtr.Zero)
            {
                PInvoke.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }

            if (_overlayWindow != null)
            {
                _overlayWindow.Close();
                _overlayWindow = null;
            }
        }

        public void UpdateSettings()
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.UpdateEffect();
                UpdateOverlayBounds();
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == PInvoke.EVENT_SYSTEM_FOREGROUND)
            {
                HandleFocusChange(hwnd);
            }
        }

        private void HandleFocusChange(IntPtr foregroundHwnd)
        {
            if (_overlayWindow == null) return;
            if (!_settingsService.IsEnabled) return;

            var overlayHwnd = WinRT.Interop.WindowNative.GetWindowHandle(_overlayWindow);

            if (foregroundHwnd == overlayHwnd) return;
            if (foregroundHwnd == IntPtr.Zero) return;

            // Ensure overlay covers the screen
            UpdateOverlayBounds();

            // Place overlay behind the foreground window
            // flags: SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
            // We use SWP_NOACTIVATE to ensure we don't steal focus back (though we are just moving Z-order)

            // Logic:
            // 1. Move Overlay to be just under ForegroundWindow.
            // Note: If ForegroundWindow is TopMost, Overlay will try to be TopMost?
            // SetWindowPos(Overlay, Foreground, ...) puts Overlay *after* Foreground.

            PInvoke.SetWindowPos(overlayHwnd, foregroundHwnd, 0, 0, 0, 0, PInvoke.SWP_NOMOVE | PInvoke.SWP_NOSIZE | PInvoke.SWP_NOACTIVATE);
        }

        private void UpdateOverlayBounds()
        {
            if (_overlayWindow == null) return;

            int x, y, w, h;

            if (_settingsService.OverlayWidth > 0 && _settingsService.OverlayHeight > 0)
            {
                // Custom size - center on primary screen
                int screenW = PInvoke.GetSystemMetrics(PInvoke.SM_CXSCREEN);
                int screenH = PInvoke.GetSystemMetrics(PInvoke.SM_CYSCREEN);
                
                w = _settingsService.OverlayWidth;
                h = _settingsService.OverlayHeight;
                x = (screenW - w) / 2;
                y = (screenH - h) / 2;
                
                // Ensure we don't go off-screen too wildly if size is huge?
                // Actually user said "setting too large Width/Height (e.g. 20000) makes window disappear".
                // If w=20000, x = (1920-20000)/2 = -9040.
                // Window rect: -9040 to 10960.
                // This should cover the screen.
                // However, Windows might have limits on window coordinates or rendering surfaces.
                // Let's clamp the size to something reasonable if it's larger than virtual screen?
                // Or maybe just ensure the center is correct.
                // If the window is too large, maybe we should just limit it to virtual screen size?
                // But user might want it to span multiple monitors manually.
                
                // Let's try to limit the coordinates to 16-bit signed integer range (-32768 to 32767) just in case SetWindowPos has issues?
                // But 20000 is within range.
                
                // Maybe the issue is that the window is created with 0 size initially or something?
                // No, UpdateSize sets it.
                
                // Let's try to ensure at least the top-left corner is somewhat reasonable?
                // No, if we want center, top-left must be far left.
                
                // Maybe the issue is Z-order when size is huge?
                // Or maybe the user means "disappear" as in "not visible".
                
                // Let's try to clamp the max width/height to Virtual Screen * 2?
                int maxW = PInvoke.GetSystemMetrics(PInvoke.SM_CXVIRTUALSCREEN) * 2;
                int maxH = PInvoke.GetSystemMetrics(PInvoke.SM_CYVIRTUALSCREEN) * 2;
                
                if (w > maxW) w = maxW;
                if (h > maxH) h = maxH;
                
                // Recalculate x,y
                x = (screenW - w) / 2;
                y = (screenH - h) / 2;
            }
            else
            {
                // Auto / Fullscreen
                x = PInvoke.GetSystemMetrics(PInvoke.SM_XVIRTUALSCREEN);
                y = PInvoke.GetSystemMetrics(PInvoke.SM_YVIRTUALSCREEN);
                w = PInvoke.GetSystemMetrics(PInvoke.SM_CXVIRTUALSCREEN);
                h = PInvoke.GetSystemMetrics(PInvoke.SM_CYVIRTUALSCREEN);
            }

            _overlayWindow.UpdateSize(x, y, w, h);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
