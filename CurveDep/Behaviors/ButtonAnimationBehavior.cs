using Microsoft.Maui.Controls;

namespace CurveDep.Behaviors
{
    public class ButtonAnimationBehavior : Behavior<Button>
    {
        protected override void OnAttachedTo(Button button)
        {
            button.Pressed += OnButtonPressed;
            button.Released += OnButtonReleased;
            base.OnAttachedTo(button);
        }

        protected override void OnDetachingFrom(Button button)
        {
            button.Pressed -= OnButtonPressed;
            button.Released -= OnButtonReleased;
            base.OnDetachingFrom(button);
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