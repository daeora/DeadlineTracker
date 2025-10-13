namespace DeadlineTracker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override async void OnStart() { await EnsureStartRouteAsync(); }
    protected override async void OnResume() { await EnsureStartRouteAsync(); }

    static async Task EnsureStartRouteAsync()
    {
        var loggedIn = Services.UserService.GetCurrentUserId() is not null;
        await Shell.Current.GoToAsync(loggedIn ? "//home" : "//login");
    }
}