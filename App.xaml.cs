using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WindowsFocuser.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsFocuser
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static SettingsService Settings { get; private set; }
        public static FocusService FocusService { get; private set; }
        public static HotKeyService HotKeyService { get; private set; }
        public static TrayIconService TrayIconService { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Settings = new SettingsService();
            FocusService = new FocusService(Settings);
            FocusService.Start();

            m_window = new MainWindow();
            m_window.Activate();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);

            HotKeyService = new HotKeyService(hWnd, Settings, () => {
                Settings.IsEnabled = !Settings.IsEnabled;
                Settings.Save();
                if (Settings.IsEnabled) FocusService.Start(); else FocusService.Stop();

                // Update UI
                if (m_window is MainWindow mw)
                {
                    mw.UpdateToggleState();
                }
            });
            HotKeyService.Register();

            TrayIconService = new TrayIconService(hWnd);
            TrayIconService.Initialize();

            m_window.Closed += (s, e) => {
                FocusService.Stop();
                HotKeyService.Dispose();
                TrayIconService.Dispose();
            };
        }

        private Window m_window;
    }
}
