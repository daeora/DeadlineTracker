namespace DeadlineTracker
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("LoginPage", typeof(MainPage));
            Routing.RegisterRoute("Home", typeof(Yleisnakyma));
            Routing.RegisterRoute("ProjectCreate", typeof(ProjectCreatePage));
            // UUSI: muokkaussivu
            Routing.RegisterRoute("ProjectEdit", typeof(ProjectEditPage));
        }
    }
}
