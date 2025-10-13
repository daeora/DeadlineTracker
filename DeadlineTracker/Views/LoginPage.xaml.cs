using DeadlineTracker.Services;

namespace DeadlineTracker.Views;

public partial class LoginPage : ContentPage
{
    readonly UserService _users = new();

    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Error("");
        UsernameEntry.Focus();
    }

    void Error(string? msg)
    {
        ErrorLabel.Text = msg ?? "";
        ErrorLabel.IsVisible = !string.IsNullOrEmpty(msg);
    }

    async void OnSignInClicked(object? sender, EventArgs e)
    {
        var name = UsernameEntry.Text?.Trim() ?? "";
        var (ok, err) = await _users.SignInAsync(name);
        if (!ok) { Error(err); return; }

        await Shell.Current.GoToAsync("//home");
    }

    async void OnCreateClicked(object? sender, EventArgs e)
    {
        var name = UsernameEntry.Text?.Trim() ?? "";
        if (!UserService.IsValid(name)) { Error("Tarkista käyttäjänimi."); return; }

        var confirm = await DisplayAlert("Luo käyttäjä", $"Luodaanko käyttäjä “{name}”?", "Kyllä", "Peruuta");
        if (!confirm) return;

        var (ok, err) = await _users.CreateAsync(name);
        if (!ok) { Error(err); return; }

        await Shell.Current.GoToAsync("//home");
    }
}