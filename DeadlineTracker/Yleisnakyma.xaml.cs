namespace DeadlineTracker;

public partial class Yleisnakyma : ContentPage
{
    public Yleisnakyma(string username)
    {
        InitializeComponent();
        TervetuloaTeksti.Text = $"Tervetuloa {username}";
    }

    private async void LogOut_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Vahvistus",
            "Haluatko varmasti kirjautua ulos?",
            "Kyllä", "Peruuta");

        if (!confirm)
            return;

        if (Application.Current != null)
        {
            Application.Current.MainPage = new AppShell();
        }
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//MainPage");
        }
    }

    private async void AddProject_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProjectCreatePage));
    }
}