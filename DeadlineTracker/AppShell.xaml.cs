namespace DeadlineTracker;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // ei pakollinen, mutta selkeyden vuoksi:
        Routing.RegisterRoute(nameof(ProjectCreatePage), typeof(ProjectCreatePage));
    }
}