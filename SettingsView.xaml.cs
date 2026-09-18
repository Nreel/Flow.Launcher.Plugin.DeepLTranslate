using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.DeepLTranslate
{
    public partial class SettingsView : UserControl
    {
        private sealed record Option(string Code, string Display);

        private readonly PluginInitContext _context;
        private readonly Settings _settings;
        private bool _loading;

        public SettingsView(PluginInitContext context, Settings settings)
        {
            InitializeComponent();
            _context = context;
            _settings = settings;

            PopulateDropdowns();
            LoadCurrentValues();
        }

        private void PopulateDropdowns()
        {
            EndpointCombo.ItemsSource = new[]
            {
                new Option("free", "Free — api-free.deepl.com"),
                new Option("pro", "Pro — api.deepl.com"),
            };
            EndpointCombo.DisplayMemberPath = "Display";

            var sources = new[] { new Option(Languages.AutoDetectCode, Languages.AutoDetectName) }
                .Concat(Languages.SourceLanguages.Select(x => new Option(x.Code, x.Name)))
                .ToList();
            SourceCombo.ItemsSource = sources;
            SourceCombo.DisplayMemberPath = "Display";

            TargetCombo.ItemsSource = Languages.TargetLanguages
                .Select(x => new Option(x.Code, x.Name))
                .ToList();
            TargetCombo.DisplayMemberPath = "Display";

            LanguageCodesText.Text = string.Join("   ",
                Languages.TargetLanguages.Select(l => $"{l.Aliases[0]} = {l.Name}"));
        }

        private void LoadCurrentValues()
        {
            _loading = true;
            try
            {
                EndpointCombo.SelectedIndex = _settings.UseFreeEndpoint ? 0 : 1;

                SourceCombo.SelectedIndex = SelectByCode(SourceCombo, _settings.DefaultSourceLanguage);
                TargetCombo.SelectedIndex = SelectByCode(TargetCombo, _settings.DefaultTargetLanguage);

                UpdateApiKeyStatus();
            }
            finally
            {
                _loading = false;
            }
        }

        private static int SelectByCode(ComboBox combo, string code)
        {
            for (var i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is Option o &&
                    string.Equals(o.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return 0;
        }

        private void UpdateApiKeyStatus()
        {
            var configured = !string.IsNullOrEmpty(_settings.EncryptedApiKey);
            ApiKeyStatus.Text = configured ? "✓ API key configured" : "No key set";
            ApiKeyStatus.Foreground = configured ? Brushes.Green : Brushes.Gray;
        }

        /// <summary>
        /// Flow Launcher gives this panel an unlimited height inside the Plugins list, so
        /// <see cref="ScrollViewer.ScrollableHeight"/> is 0 and WPF's ScrollViewer would still swallow
        /// every wheel event (it always marks them handled). Route the wheel to whatever can actually
        /// scroll instead: the panel itself, a scrollable element inside it, or Flow's plugin list.
        /// </summary>
        private void SettingsView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;

            // An open dropdown owns the wheel: scroll its own list and never the page behind it.
            // The search must stay inside this panel - walking further would find Flow's plugin
            // list and hand the wheel to it, which is exactly what must not happen here.
            if (IsAnyDropDownOpen())
            {
                if (WheelScrollChaining.TryFindScrollableAncestor(source, e.Delta, out var dropDown, this))
                    WheelScrollChaining.Forward(dropDown!, e);
                else
                    e.Handled = true; // nothing inside the open dropdown can scroll: keep the page still

                return;
            }

            if (!WheelScrollChaining.IsWithinComboBox(source, this))
            {
                // Something inside the panel (the panel itself or e.g. a text box content host) can
                // scroll: keep WPF's default handling and let it move.
                if (WheelScrollChaining.TryFindScrollableAncestor(source, e.Delta, out _, PanelScroll))
                    return;
            }
            else if (WheelScrollChaining.TryFindScrollableAncestor(source, e.Delta, out var inner, PanelScroll))
            {
                // The wheel is over a closed dropdown: scroll instead of letting the ComboBox change
                // the selected language, and keep the ComboBox out of the re-raised event's route.
                WheelScrollChaining.Forward(inner!, e);
                return;
            }

            // Nothing here can scroll (the usual case), so hand the wheel to Flow's plugin list.
            if (WheelScrollChaining.TryFindScrollableAncestor(
                    VisualTreeHelper.GetParent(PanelScroll), e.Delta, out var outer))
            {
                WheelScrollChaining.Forward(outer!, e);
            }
        }

        private bool IsAnyDropDownOpen() =>
            EndpointCombo.IsDropDownOpen || SourceCombo.IsDropDownOpen || TargetCombo.IsDropDownOpen;

        private void ApiKeyBox_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            SaveApiKey();
        }

        private void ClearKeyButton_Click(object sender, RoutedEventArgs e)
        {
            ApiKeyBox.Clear();
            _settings.EncryptedApiKey = string.Empty;
            SaveSettings();
            UpdateApiKeyStatus();
        }

        private void SaveApiKey()
        {
            var password = ApiKeyBox.Password;
            if (string.IsNullOrWhiteSpace(password))
                return;

            _settings.EncryptedApiKey = CredentialProtector.Protect(password);
            ApiKeyBox.Clear(); // never keep the key visible after entry
            SaveSettings();
            UpdateApiKeyStatus();
        }

        private void EndpointCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || EndpointCombo.SelectedItem is not Option o)
                return;

            _settings.UseFreeEndpoint = o.Code == "free";
            SaveSettings();
        }

        private void SourceCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || SourceCombo.SelectedItem is not Option o)
                return;

            _settings.DefaultSourceLanguage = o.Code;
            SaveSettings();
        }

        private void TargetCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || TargetCombo.SelectedItem is not Option o)
                return;

            _settings.DefaultTargetLanguage = o.Code;
            SaveSettings();
        }

        private void SaveSettings()
        {
            _context.API.SaveSettingJsonStorage<Settings>();
        }
    }
}
