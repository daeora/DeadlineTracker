using System;
using Microsoft.Maui.Controls;
using DeadlineTracker.Services;
using DeadlineTracker.ViewModels;
using DeadlineTracker.Models;

namespace DeadlineTracker;

public partial class Yleisnakyma : ContentPage
{
    private readonly ProjectListViewModel vm;
    // HUOM: nyt EI ole string username parametria!
    public Yleisnakyma()
    {
        InitializeComponent();
        vm = new ProjectListViewModel();
        BindingContext = vm;

        // asetetaan tervetuloteksti kirjautuneen mukaan
        if (!string.IsNullOrWhiteSpace(Session.CurrentUsername))
        {
            TervetuloaTeksti.Text = $"Tervetuloa {Session.CurrentUsername}";
        }
        else
        {
            TervetuloaTeksti.Text = "Tervetuloa";
        }
    }


    private async void LogOut_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Vahvistus",
            "Haluatko varmasti kirjautua ulos?",
            "Kyll‰", "Peruuta");

        if (!confirm)
            return;

        // tyhjenn‰ session
        Session.CurrentUserId = 0;
        Session.CurrentUsername = string.Empty;

        // EI en‰‰ vaihdeta MainPagea k‰sin uuteen AppShelliin,
        // koska meill‰ on sama Shell koko ajan k‰ynniss‰.

        // takaisin kirjaudusivulle
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void AddProject_Clicked(object sender, EventArgs e)
    {
        // siirry projektin/teht‰v‰n luontiin (ProjectCreatePage)
        await Shell.Current.GoToAsync("ProjectCreate");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await vm.LoadProjectsAsync(Session.CurrentUserId, force: true, all: vm.ShowAll /* jos teit napin */);
    }

    private async void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return; // reagoi vain kun merkataan valmiiksi
        if ((sender as BindableObject)?.BindingContext is Tehtava t)
            await vm.CompleteTaskAsync(t);
    }

    // avataan projektin muokkausn‰kym‰ kun projektikorttia napautetaan
    private async void OpenProjectEdit_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int iid)
            await Shell.Current.GoToAsync($"ProjectEdit?id={iid}");
        else if (e.Parameter is long lid)
            await Shell.Current.GoToAsync($"ProjectEdit?id={lid}");
    }

    private async void ToggleAll_Clicked(object sender, EventArgs e)
    {
        vm.ShowAll = !vm.ShowAll;
        await vm.LoadProjectsAsync(Session.CurrentUserId, force: true, all: vm.ShowAll);
    }

}