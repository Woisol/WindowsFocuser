using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using WindowsFocuser.Helpers;

namespace WindowsFocuser
{
    public sealed partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            this.InitializeComponent();

            // Remove borders and title bar
            var appWindow = this.AppWindow;
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            }
            appWindow.SetPresenter(OverlappedPresenter.CreateForContextMenu());

            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }

            // Make it click-through and tool window (hide from alt-tab)
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            int exStyle = PInvoke.GetWindowLong(hWnd, PInvoke.GWL_EXSTYLE);
            PInvoke.SetWindowLong(hWnd, PInvoke.GWL_EXSTYLE, exStyle | PInvoke.WS_EX_TRANSPARENT | PInvoke.WS_EX_TOOLWINDOW | PInvoke.WS_EX_LAYERED);

            // Initial Effect
            UpdateEffect();
        }

        public void UpdateEffect()
        {
            var settings = App.Settings;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Parse color
            Windows.UI.Color overlayColor = Microsoft.UI.Colors.Black;
            try
            {
                if (!string.IsNullOrEmpty(settings.OverlayColor))
                {
                    var c = Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), settings.OverlayColor);
                    if (c is Windows.UI.Color color) overlayColor = color;
                }
            }
            catch {}

            if (settings.EffectType == "Acrylic")
            {
                if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
                {
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
                    // Use user color with its own alpha for tint
                    RootGrid.Background = new SolidColorBrush(overlayColor);
                }
                else
                {
                    RootGrid.Background = new SolidColorBrush(overlayColor);
                }
            }
            else if (settings.EffectType == "Mica")
            {
                if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
                {
                    this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop() { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base };
                    // Use user color with its own alpha for tint
                    RootGrid.Background = new SolidColorBrush(overlayColor);
                }
                else
                {
                    RootGrid.Background = new SolidColorBrush(overlayColor);
                }
            }
            else
            {
                this.SystemBackdrop = null;
                // For Dim, we force opaque color because SetLayeredWindowAttributes handles the alpha.
                // If we let user pick transparent color, it will be double transparent.
                // But let's trust the user or just force alpha 255 for Dim mode?
                // Let's force alpha 255 for Dim mode to ensure DimOpacity works as expected.
                var opaqueColor = overlayColor;
                opaqueColor.A = 255;
                RootGrid.Background = new SolidColorBrush(opaqueColor);
            }

            SetOpacity(settings.DimOpacity);
        }

        public void SetOpacity(double opacity)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            byte alpha = (byte)(opacity * 255);

            // If using SystemBackdrop, we might not want to use LayeredWindowAttributes for alpha,
            // because it fades the whole window including the blur.
            // But for "Dim" effect, it is exactly what we want.

            PInvoke.SetLayeredWindowAttributes(hWnd, 0, alpha, PInvoke.LWA_ALPHA);
        }

        public void UpdateSize(int x, int y, int width, int height)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            PInvoke.SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, PInvoke.SWP_NOACTIVATE | PInvoke.SWP_NOZORDER);
        }
    }
}
