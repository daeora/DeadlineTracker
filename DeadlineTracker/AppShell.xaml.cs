namespace DeadlineTracker
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ProjectCreatePage), typeof(ProjectCreatePage));
        }
    }
}
