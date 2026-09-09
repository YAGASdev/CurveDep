namespace CurveDep
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell())
            {
                Width = 1160,
                Height = 840,
                MinimumWidth = 1160,
                MinimumHeight = 840
            };

            return window;
        }
    }
}