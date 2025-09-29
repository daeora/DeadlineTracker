using Microsoft.UI.Xaml.Controls;

namespace DeadlineTracker
{
    public partial class MainPage : ContentPage
    {
    

        public MainPage()
        {
            InitializeComponent();
        }

        // Kirjaudu sisään nappi. Salasana ja tunnus on "Admin"
        private async void login_Clicked(object sender, EventArgs e)
        {

            if (Username.Text == "Admin" && Password.Text == "Admin")
            {
                await Navigation.PushAsync(new Yleisnakyma(Username.Text)); //hakee myös käyttäjän usernamen mukaisesti nimen pääsivulle. Voi testata poistamalla if elsen.
            }

            //Jos salasana on väärin heittää ikkunnan jossa kyseinen teksti, Tähän pitää tehdä virheen tarkistus vielä.
            else 
            {
                await DisplayAlert("Virhe", "Käyttäjätunnus ja salasana on > Admin <", "OK");            
            }
           
        }

        private void NewAccount_Clicked(object sender, EventArgs e)
        {

        }
    }

}
