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
                Height = 650,
                MinimumWidth = 1160,
                MinimumHeight = 650
            };

            return window;
        }
    }
}