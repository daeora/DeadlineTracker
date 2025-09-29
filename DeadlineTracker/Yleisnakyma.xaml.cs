namespace DeadlineTracker;

public partial class Yleisnakyma : ContentPage
{
	public Yleisnakyma(string username)
	{
		InitializeComponent();
		TervetuloaTeksti.Text = $"Tervetuloa {username}";
	}
}