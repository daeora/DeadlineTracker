namespace DeadlineTracker
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            UserAppTheme = AppTheme.Light;  // ei vaikuta otsikkopalkkiin, vain sovelluksen teemoihin
            MainPage = new AppShell();
        }
    }
}
