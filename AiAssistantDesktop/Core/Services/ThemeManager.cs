using System;
using System.Linq;
using System.Windows;

namespace AiAssistantDesktop.Core.Services
{
    public class ThemeManager
    {
        private static readonly string[] _availableThemes = { "Dark", "Light", "Cyber" };
        public string[] AvailableThemes => _availableThemes;

        public void ApplyTheme(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName) || !_availableThemes.Contains(themeName))
                themeName = "Dark";

            try
            {
                var app = Application.Current;
                app.Resources.MergedDictionaries.Clear();

                var uri = new Uri($"pack://application:,,,/Themes/{themeName}Theme.xaml", UriKind.Absolute);
                var dict = new ResourceDictionary { Source = uri };
                app.Resources.MergedDictionaries.Add(dict);

                // Сохраняем в настройки (нужно добавить Settings в проект)
                Properties.Settings.Default.Theme = themeName;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme error: {ex.Message}");
            }
        }

        public string GetCurrentTheme()
        {
            try
            {
                return Properties.Settings.Default.Theme ?? "Dark";
            }
            catch
            {
                return "Dark";
            }
        }
    }
}