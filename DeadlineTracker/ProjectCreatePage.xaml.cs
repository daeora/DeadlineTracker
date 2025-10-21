namespace DeadlineTracker;

public partial class ProjectCreatePage : ContentPage
{
	public ProjectCreatePage()
	{
		InitializeComponent();
	}

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}