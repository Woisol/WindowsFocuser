using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsFocuser
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool _isInitializing = true;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(500, 800));
            this.AppWindow.Closing += AppWindow_Closing;

            // Load initial values
            var settings = App.Settings;
            EnableSwitch.IsOn = settings.IsEnabled;
            StartOnBootSwitch.IsOn = settings.StartOnBoot;
            OpacitySlider.Value = settings.DimOpacity;
            SettingsPathText.Text = settings.GetSettingsFilePath();

            WidthBox.Value = settings.OverlayWidth;
            HeightBox.Value = settings.OverlayHeight;

            if (settings.EffectType == "Acrylic") EffectComboBox.SelectedIndex = 1;
            else if (settings.EffectType == "Mica") EffectComboBox.SelectedIndex = 2;
            else if (settings.EffectType == "Blur") EffectComboBox.SelectedIndex = 3;
            else EffectComboBox.SelectedIndex = 0;

            UpdateHotKeyDisplay();
            UpdateColorDisplay();

            _isInitializing = false;
        }

        private void UpdateHotKeyDisplay()
        {
            var settings = App.Settings;
            var parts = new List<string>();
            if ((settings.HotKeyModifiers & 0x0002) != 0) parts.Add("Ctrl");
            if ((settings.HotKeyModifiers & 0x0001) != 0) parts.Add("Alt");
            if ((settings.HotKeyModifiers & 0x0004) != 0) parts.Add("Shift");
            if ((settings.HotKeyModifiers & 0x0008) != 0) parts.Add("Win");

            if (settings.HotKeyKey != 0)
            {
                parts.Add(((VirtualKey)settings.HotKeyKey).ToString());
            }

            HotKeyBox.Text = string.Join(" + ", parts);
        }

        private void UpdateColorDisplay()
        {
            try
            {
                var colorStr = App.Settings.OverlayColor;
                if (!string.IsNullOrEmpty(colorStr))
                {
                    var color = Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Windows.UI.Color), colorStr);
                    if (color is Windows.UI.Color c)
                    {
                        ColorPreview.Background = new SolidColorBrush(c);
                        OverlayColorPicker.Color = c;
                    }
                }
            }
            catch { }
        }

        public void UpdateToggleState()
        {
            // Called from App when hotkey toggles state
            DispatcherQueue.TryEnqueue(() =>
            {
                _isInitializing = true;
                EnableSwitch.IsOn = App.Settings.IsEnabled;
                _isInitializing = false;
            });
        }

        private void EnableSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            App.Settings.IsEnabled = EnableSwitch.IsOn;
            App.Settings.Save();

            if (App.Settings.IsEnabled)
            {
                App.FocusService.Start();
            }
            else
            {
                App.FocusService.Stop();
            }
        }

        private void StartOnBootSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            App.Settings.StartOnBoot = StartOnBootSwitch.IsOn;
            App.Settings.Save();
        }

        private void OpacitySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_isInitializing) return;

            App.Settings.DimOpacity = OpacitySlider.Value;
            App.Settings.Save();
            App.FocusService.UpdateSettings();
        }

        private void EffectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (EffectComboBox.SelectedIndex == 1) App.Settings.EffectType = "Acrylic";
            else if (EffectComboBox.SelectedIndex == 2) App.Settings.EffectType = "Mica";
            else if (EffectComboBox.SelectedIndex == 3) App.Settings.EffectType = "Blur";
            else App.Settings.EffectType = "Dim";


            App.Settings.Save();
            App.FocusService.UpdateSettings();
        }


        private void SizeBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isInitializing) return;

            App.Settings.OverlayWidth = (int)WidthBox.Value;
            App.Settings.OverlayHeight = (int)HeightBox.Value;
            App.Settings.Save();
            App.FocusService.UpdateSettings();
        }

        private void HotKeyBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;

            var key = e.Key;
            // Ignore modifier keys alone
            if (key == VirtualKey.Control || key == VirtualKey.Menu || key == VirtualKey.Shift || key == VirtualKey.LeftWindows || key == VirtualKey.RightWindows)
            {
                return;
            }

            uint mods = 0;
            var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            if ((state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down) mods |= 0x0002;

            state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
            if ((state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down) mods |= 0x0001;

            state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
            if ((state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down) mods |= 0x0004;

            state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows);
            if ((state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down) mods |= 0x0008;
            state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows);
            if ((state & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down) mods |= 0x0008;

            // If Shift is pressed, e.Key might be the shifted character (e.g. 'A' instead of 'a' or symbol).
            // VirtualKey is usually the same for letters.
            // But for numbers, Shift+1 is '!', which has a different VK? No, VK_1 is same.
            // However, we should ensure we capture the base key.

            App.Settings.HotKeyModifiers = mods;
            App.Settings.HotKeyKey = (uint)key;
            App.Settings.Save();

            UpdateHotKeyDisplay();
            App.HotKeyService.UpdateHotKey();
        }

        private void ClearHotKey_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.HotKeyModifiers = 0;
            App.Settings.HotKeyKey = 0;
            App.Settings.Save();
            UpdateHotKeyDisplay();
            App.HotKeyService.UpdateHotKey();
        }

        private void OverlayColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (_isInitializing) return;

            var c = args.NewColor;
            App.Settings.OverlayColor = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
            ColorPreview.Background = new SolidColorBrush(c);

            // Debounce save? For now just save.
            App.Settings.Save();
            App.FocusService.UpdateSettings();
        }

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            args.Cancel = true;
            sender.Hide();
        }
    }
}
