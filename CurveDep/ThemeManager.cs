using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace CurveDep
{
    public partial class ThemeManager : INotifyPropertyChanged
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        private bool _isDarkTheme = false;

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged(nameof(IsDarkTheme));
                    OnPropertyChanged(nameof(PageBackgroundColor));
                    OnPropertyChanged(nameof(TextColor));
                    OnPropertyChanged(nameof(EntryTextColor));
                    OnPropertyChanged(nameof(ThemeButtonColor));
                    OnPropertyChanged(nameof(ThemeButtonText));
                    OnPropertyChanged(nameof(StepperColor));
                }
            }
        }

        public Color PageBackgroundColor => _isDarkTheme ? Color.FromArgb("#1a1a1a") : Color.FromArgb("#ffffff");
        public Color TextColor => _isDarkTheme ? Color.FromArgb("#ffffff") : Color.FromArgb("#000000");
        public Color EntryTextColor => _isDarkTheme ? Color.FromArgb("#ffffff") : Color.FromArgb("#000000");
        public Color ThemeButtonColor => _isDarkTheme ? Color.FromArgb("#555555") : Color.FromArgb("#9880e5");
        public Color StepperColor => _isDarkTheme ? Colors.White : Colors.Black;
        public string ThemeButtonText => _isDarkTheme ? "🌙 Тёмная" : "☀️ Светлая";

        public void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}