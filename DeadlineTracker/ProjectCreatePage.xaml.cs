using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using DeadlineTracker.Services;
using Microsoft.Maui.Controls;

namespace DeadlineTracker;

public partial class ProjectCreatePage : ContentPage
{
    // Listat UI:lle
    public ObservableCollection<TaskRow> Tasks { get; } = new();
    public ObservableCollection<MemberRow> Participants { get; } = new();

    // Osallistuja-dropdown
    private readonly UserService _users = new(AuthService.ConnectionString);
    private List<UserDto> _allUsers = new();
    public ObservableCollection<UserDto> FilteredUsers { get; } = new();

    public bool AreUserSuggestionsVisible { get; set; }
    public string? SelectedUserName { get; set; }
    public int? SelectedUserId { get; set; }
    public bool HasSelectedUser => SelectedUserId.HasValue;

    private readonly ProjectService _projects = new(AuthService.ConnectionString);

    public ProjectCreatePage()
    {
        InitializeComponent();
        BindingContext = this;

        // Esimerkkidata
        Tasks.Add(new TaskRow { Title = "Esimerkkitehtävä" });
        Participants.Add(new MemberRow { FullName = "Käyttäjä 1"});
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _allUsers = await _users.GetAllAsync(); // vain talteen
        FilteredUsers.Clear();                  // ei täytetä
        AreUserSuggestionsVisible = false;
        OnPropertyChanged(nameof(AreUserSuggestionsVisible));
    }

    // ----- TEHTÄVÄT -----
    private void AddTask_Clicked(object sender, EventArgs e)
    {
        var text = NewTaskEntry?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        Tasks.Add(new TaskRow { Title = text, Done = false });
        NewTaskEntry.Text = string.Empty;
    }

    private async void DeleteTask_Clicked(object sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is TaskRow row)
        {
            var ok = await DisplayAlert("Poista tehtävä",
                                        $"Poistetaanko?",
                                        "Poista", "Peruuta");
            if (ok)
                Tasks.Remove(row);
        }
    }

    // ----- OSALLISTUJAT HAKU -----
    // Näytä ehdotukset vain kun on tekstiä
    private void ParticipantSearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? "").Trim();
        FilteredUsers.Clear();

        if (q.Length >= 1)
        {
            foreach (var u in _allUsers.Where(u => u.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
                FilteredUsers.Add(u);

            AreUserSuggestionsVisible = FilteredUsers.Count > 0;
        }
        else
        {
            AreUserSuggestionsVisible = false;
        }
        OnPropertyChanged(nameof(AreUserSuggestionsVisible));
    }

    // Ei näytä hakutuloksia, kun kenttä ei ole aktiivinen
    private void ParticipantSearch_Unfocused(object sender, FocusEventArgs e)
    {
        AreUserSuggestionsVisible = false;
        OnPropertyChanged(nameof(AreUserSuggestionsVisible));
    }

    // Yksi klikkaus nimestä lisää osallistujan
    private async void UserSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is UserDto u)
        {
            // Estä duplikaatit id:n perusteella
            if (Participants.Any(p => p.UserId == u.Id ||
                                      string.Equals(p.FullName, u.Name, StringComparison.OrdinalIgnoreCase)))
            {
                await DisplayAlert("Jo lisätty", $"{u.Name} on jo osallistujissa.", "OK");
            }
            else
            {
                Participants.Add(new MemberRow { UserId = u.Id, FullName = u.Name, Role = "" });
            }

            // Reset UI, tyhjentää kentän ja piilottaa ehdotukset
            ((CollectionView)sender).SelectedItem = null;
            ParticipantSearchEntry.Text = "";
            AreUserSuggestionsVisible = false;
            OnPropertyChanged(nameof(AreUserSuggestionsVisible));
            ParticipantSearchEntry.Unfocus();
        }
    }
    // ----- Osallistujan poisto -----
    private async void DeleteParticipant_Clicked(object sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is MemberRow row)
        {
            var ok = await DisplayAlert("Poista osallistuja",
                                        $"Poistetaanko {row.FullName}?",
                                        "Poista", "Peruuta");
            if (ok)
                Participants.Remove(row);
        }
    }

    // ----- TALLENNUS / PERUUTA -----
    private async void Save_Clicked(object sender, EventArgs e)
    {
        try
        {
            var name = NameEntry.Text?.Trim() ?? "";
            var desc = DescEditor.Text?.Trim() ?? "";
            var start = StartDatePicker.Date;
            var end = EndDatePicker.Date;

            var taskDtos = Tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .Select(t => (title: t.Title.Trim(), done: t.Done))
                .ToList();

            var memberNames = Participants
                .Where(p => !string.IsNullOrWhiteSpace(p.FullName))
                .Select(p => p.FullName.Trim())
                .ToList();

            var id = await _projects.CreateProjectAsync(name, desc, start, end, taskDtos, memberNames);

            await DisplayAlert("OK", $"Projekti luotu (ID {id}).", "Sulje");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", ex.Message, "Sulje");
        }
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}

// ------- rivamallit -------
public class TaskRow { public string Title { get; set; } = ""; public bool Done { get; set; } }
public class MemberRow
{
    public int? UserId { get; set; }       
    public string FullName { get; set; } = "";
    public string? Role { get; set; }
}

