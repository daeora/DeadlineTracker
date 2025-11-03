using System;
using Microsoft.Maui.Controls;
using DeadlineTracker.Services;

namespace DeadlineTracker
{
    public partial class MainPage : ContentPage
    {
        // Huom: luodaan AuthService, joka puhuu tietokannan kanssa
        private AuthService _auth = new AuthService();

        public MainPage()
        {
            InitializeComponent();
        }

        // Tää korvaa vanhan Admin/Admin-loginin
        private async void login_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Luetaan käyttäjänimi kentästä
                string typedName = Username.Text?.Trim();

                if (string.IsNullOrWhiteSpace(typedName))
                {
                    await DisplayAlert("Virhe", "Anna käyttäjänimi 🙃", "OK");
                    return;
                }

                // 1. Tarkista löytyykö käyttäjä kannasta, jos ei -> luo se
                int userId = await _auth.LoginOrCreateUserAsync(typedName);

                // 2. Tallenna kuka on kirjautunut
                Session.CurrentUserId = userId;
                Session.CurrentUsername = typedName;

                // 3. Hyppää sovelluksen pääsivulle (Yleisnäkymä)
                await Shell.Current.GoToAsync("//Home");
            }
            catch (Exception ex)
            {
                // Jos tietokanta kaatuu tms
                await DisplayAlert("Virhe", ex.Message, "OK");
            }
        }

        // Ei tarvita enää erillistä uuden käyttäjän luontia
        private void NewAccount_Clicked(object sender, EventArgs e)
        {
            // jätetään tyhjäksi tai poistetaan myöhemmin
        }
    }
}
