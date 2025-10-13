using DeadlineTracker.Models;
using DeadlineTracker.Services;

namespace DeadlineTracker.Views;

public partial class HomePage : ContentPage
{
    public HomePage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var userId = UserService.GetCurrentUserId();
        if (userId is null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        var db = await Database.GetAsync();
        var user = await db.GetAsync<User>(userId.Value);
        WelcomeLabel.Text = $"Tervetuloa, {user.Username}!";
    }

    async void OnLogout(object? sender, EventArgs e)
    {
        UserService.Logout();
        await Shell.Current.GoToAsync("//login");
    }
}