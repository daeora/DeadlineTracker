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
        var btn = (Button)sender;

        await btn.ScaleTo(0.9, 80, Easing.CubicOut);
        await btn.ScaleTo(1.0, 80, Easing.CubicIn);

        bool confirm = await DisplayAlert(
            "Vahvistus",
            "Haluatko varmasti kirjautua ulos?",
            "Kyllä", "Peruuta");

        if (!confirm)
            return;

        // tyhjennä session
        Session.CurrentUserId = 0;
        Session.CurrentUsername = string.Empty;

        // EI enää vaihdeta MainPagea käsin uuteen AppShelliin,
        // koska meillä on sama Shell koko ajan käynnissä.
        // takaisin kirjaudusivulle
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void AddProject_Clicked(object sender, EventArgs e)
    {

        // siirry projektin/tehtävän luontiin (ProjectCreatePage)
        await Shell.Current.GoToAsync("ProjectCreate");
    }

    protected override async void OnAppearing()
    {

        base.OnAppearing();
        TervetuloaTeksti.Text = $"Tervetuloa {Session.CurrentUsername} 👋";
        await vm.LoadProjectsAsync(Session.CurrentUserId, force: true, all: vm.ShowAll /* jos teit napin */);
    }

    private async void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (!e.Value) return; // reagoi vain kun merkataan valmiiksi
        if ((sender as BindableObject)?.BindingContext is Tehtava t)
            await vm.CompleteTaskAsync(t);
    }

    // avataan projektin muokkausnäkymä kun projektikorttia napautetaan
    private async void OpenProjectEdit_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is int iid)
            await Shell.Current.GoToAsync($"ProjectEdit?id={iid}");
        else if (e.Parameter is long lid)
            await Shell.Current.GoToAsync($"ProjectEdit?id={lid}");
    }

    //Mikähän tarkoitus tällä oli
    //private async void ToggleAll_Clicked(object sender, EventArgs e)
    //{
    //    vm.ShowAll = !vm.ShowAll;
    //    await vm.LoadProjectsAsync(Session.CurrentUserId, force: true, all: vm.ShowAll);
    //}

    private bool _completedVisible = false;

    private void ToggleCompleted_Tapped(object sender, TappedEventArgs e)
    {
        _completedVisible = !_completedVisible;
        CompletedList.IsVisible = _completedVisible;

        NuoliIkoni.Text = _completedVisible ? "\uE5CE" : "\uE5CF";
    }

}