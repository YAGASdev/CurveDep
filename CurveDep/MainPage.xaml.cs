using CurveDep.ViewModels;
using Microsoft.Maui.Controls;

namespace CurveDep
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            DamCanvas.PointSelected += OnDamCanvasPointSelected;
        }

        private void OnDamCanvasPointSelected(int index)
        {
            if (BindingContext is MVM vm)
            {
                vm.SelectPointCommand.Execute(index);
            }
        }

        private void OnThemeToggleClicked(object sender, System.EventArgs e)
        {
            ThemeManager.Instance.ToggleTheme();
        }

        private void OnButtonPressed(object sender, System.EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackgroundColor = Color.FromArgb("#2196F3");
                button.ScaleTo(0.95, 100, Easing.SinInOut);
            }
        }

        private void OnButtonReleased(object sender, System.EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackgroundColor = Color.FromArgb("#9880e5");
                button.ScaleTo(1.0, 100, Easing.SinInOut);
            }
        }
    }
}