namespace DeadlineTracker;

public partial class Yleisnakyma : ContentPage
{
    public Yleisnakyma()
    {
        InitializeComponent();
    }

    private async void CreateProject_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProjectCreatePage));
    }
}