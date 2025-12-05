using DeadlineTracker.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;        // INotifyPropertyChanged
using System.Globalization;
using System.Linq;

namespace DeadlineTracker;


/// <summary>
/// Projektin luontisivu (UI + kevyt sivukohtainen logiikka).
/// - Ei tee suoria DB-kutsuja, vaan käyttää palveluita (UserService, ProjectService).
/// - Sivulla ylläpidetään projektin luonnin väliaikaista tilaa:
///   * Tasks  = tehtäväluettelo (otsikko, valmis, eräpäivä, assigneet)
///   * Participants = projektin osallistujat (valitaan kannasta haettavista käyttäjistä)
/// - Tallennus kokoaa tilan DTO:iksi ja delegoi ProjectService.CreateProjectAsync:ille.
/// </summary>


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

    private readonly ProjectService _projects = new(AuthService.ConnectionString);

    public ProjectCreatePage()
    {
        InitializeComponent();
        BindingContext = this;

        // Esimerkkidata (!!!!Voi poistaa myöhemmin!!!!!)
        Tasks.Add(new TaskRow { Title = "Esimerkkitehtävä" });
        // Participants.Add(new MemberRow { FullName = "Käyttäjä 1"}); // (Kommentoitu pois koska käyttäjää ei oikeasti kannassa)

        // Päivittää tehtävien vastuuhenkilöhaun, kun projektin osallistujia lisätään/poistetaan
        Participants.CollectionChanged += (_, __) =>
        {
            foreach (var tr in Tasks)
            {
                if (!string.IsNullOrWhiteSpace(tr.SearchText))
                    TaskAssigneeSearch_TextChanged(
                        new Entry { BindingContext = tr, Text = tr.SearchText },
                        new TextChangedEventArgs(tr.SearchText, tr.SearchText));
            }
        };

    }

    /// <summary>
    /// Sivulle tullessa: lataa kaikki käyttäjät yhdellä kyselyllä (pohja hakuihin).
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _allUsers = await _users.GetAllAsync(); // ei sidota suoraan UI:hin
        FilteredUsers.Clear();                  // tyhjänä kunnes käyttäjä kirjoittaa
        AreUserSuggestionsVisible = false;
        OnPropertyChanged(nameof(AreUserSuggestionsVisible));
    }
    

    // -------------------------------------------------------------------------
    // TEHTÄVÄT (lisäys/poisto + per-tehtävä assignee-haku ja eräpäivälogiikka)
    // -------------------------------------------------------------------------

    private async void AddTask_Clicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;

        await btn.ScaleTo(0.9, 80, Easing.CubicOut);
        await btn.ScaleTo(1.0, 80, Easing.CubicIn);

        var text = NewTaskEntry?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        Tasks.Add(new TaskRow { Title = text, Done = false });
        NewTaskEntry.Text = string.Empty;
    }

    private async void DeleteTask_Clicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;

        await btn.ScaleTo(0.9, 80, Easing.CubicOut);
        await btn.ScaleTo(1.0, 80, Easing.CubicIn);

        if ((sender as Element)?.BindingContext is TaskRow row)
        {
            var ok = await DisplayAlert("Poista tehtävä",
                                        $"Poistetaanko?",
                                        "Poista", "Peruuta");
            if (ok)
                Tasks.Remove(row);
        }
    }

    // ----- PROJEKTIIN OSALLISTUJAT HAKU (Käyttäjät tietokannasta) -----
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
            ParticipantSearchEntry.Text = string.Empty;
            AreUserSuggestionsVisible = false;
            OnPropertyChanged(nameof(AreUserSuggestionsVisible));
        }
    }
    // ----- Osallistujan poisto -----
    private async void DeleteParticipant_Clicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;

        await btn.ScaleTo(0.9, 80, Easing.CubicOut);
        await btn.ScaleTo(1.0, 80, Easing.CubicIn);

        if ((sender as Element)?.BindingContext is MemberRow row)
        {
            var ok = await DisplayAlert("Poista osallistuja",
                                        $"Poistetaanko {row.FullName}?",
                                        "Poista", "Peruuta");
            if (ok)
                Participants.Remove(row);
        }
    }

    // --- "Eräpäivä" – estä menneet ---
    private void TaskDueDate_DateSelected(object sender, DateChangedEventArgs e)
    {
        if (sender is DatePicker dp && dp.BindingContext is TaskRow tr)
        {
            if (tr.DueDate.HasValue && tr.DueDate.Value.Date < DateTime.Today)
                tr.DueDate = DateTime.Today; // korjaa takaisin tähän päivään
        }
    }

    // --- TEHTÄVÄ: haku vastuuhenkilöön ---
    private void TaskAssigneeSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is TaskRow tr)
        {
            var q = (e.NewTextValue ?? "").Trim();
            tr.SearchText = q;

            tr.FilteredUsers.Clear();

            if (q.Length >= 1)
            {
                // 1. Kerää talteen ne ID:t, jotka on JO lisätty tähän nimenomaiseen tehtävään
                var existingIds = tr.Assignees
                    .Where(a => a.UserId.HasValue)
                    .Select(a => a.UserId.Value)
                    .ToList();

                // 2. Käy läpi projektin osallistujat
                var pool = Participants
                    .Where(p => p.UserId.HasValue)
                    .Select(p => new UserDto { Id = p.UserId!.Value, Name = p.FullName });

                // 3. Suodata: 
                //    - Nimi täsmää hakuun
                //    - JA henkilön ID ei löydy 'existingIds' listalta
                var matches = pool.Where(u =>
                    u.Name.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                    !existingIds.Contains(u.Id));

                foreach (var u in matches)
                {
                    tr.FilteredUsers.Add(u);
                }

                tr.AreUserSuggestionsVisible = tr.FilteredUsers.Count > 0;
            }
            else
            {
                tr.AreUserSuggestionsVisible = false;
            }
        }
    }

    private void TaskAssigneeSuggestion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView cv && cv.BindingContext is TaskRow tr)
        {
            if (e.CurrentSelection?.FirstOrDefault() is UserDto u)
            {

                // Lisätään henkilö listaan, jos ei jo ole
                if (!tr.Assignees.Any(a => a.UserId == u.Id))
                    tr.Assignees.Add(new MemberRow { UserId = u.Id, FullName = u.Name });

                // Resetoidaan datamalli
                tr.FilteredUsers.Clear();
                tr.AreUserSuggestionsVisible = false;
                tr.SearchText = "";

                // --- KORJATTU ENTRY-KENTÄN TYHJENNYS ---
                // Nyt etsitään Gridistä Border, ja Borderin sisältä Entry
                if (cv.Parent is Frame f && f.Parent is Grid g)
                {
                    // Etsi Border-elementti Gridin lapsista
                    var border = g.Children.OfType<Border>().FirstOrDefault();

                    // Jos Border löytyy ja sen sisältö on Entry, tyhjennä se
                    if (border != null && border.Content is Entry entry)
                    {
                        entry.Text = string.Empty;
                        entry.Unfocus(); // Valinnainen: piilottaa näppäimistön
                    }
                }
                // ----------------------------------------
            }
            cv.SelectedItem = null;
        }
    }

    private async void TaskRemoveAssignee_Clicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;

        await btn.ScaleTo(0.9, 80, Easing.CubicOut);
        await btn.ScaleTo(1.0, 80, Easing.CubicIn);

        if (sender is Element el && el.BindingContext is MemberRow mr)
        {
            // etsi rivin TaskRow
            var parentTask = (el.Parent as Element);
            while (parentTask != null && parentTask.BindingContext is not TaskRow) parentTask = parentTask.Parent as Element;
            if (parentTask?.BindingContext is TaskRow tr) tr.Assignees.Remove(mr);
        }
    }

    // ----- TALLENNUS / PERUUTA -----
    private async void Save_Clicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;

        await btn.ScaleTo(0.9, 80, Easing.CubicOut);
        await btn.ScaleTo(1.0, 80, Easing.CubicIn);

        try
        {
            var name = NameEntry.Text?.Trim() ?? "";
            var desc = DescEditor.Text?.Trim() ?? "";
            var start = StartDatePicker.Date;
            var end = EndDatePicker.Date;

            // Varmista, että tehtävien vastuuhenkilöt ovat projektin osallistujia
            var participantIds = Participants
                .Where(p => p.UserId.HasValue)
                .Select(p => p.UserId!.Value)
                .ToHashSet();

            foreach (var t in Tasks)
            {
                var assigneeId = t.Assignees.FirstOrDefault()?.UserId;
                if (assigneeId.HasValue && !participantIds.Contains(assigneeId.Value))
                    t.Assignees.Clear();   // poistetaan tehtävältä “väärä” henkilön valinta
            }

            // Tehtävät: otsikko, valmis, eräpäivä, ensimmäinen vastuuhenkilö (id)
            var taskDtos = Tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .Select(t => (
                    title: t.Title.Trim(),
                    done: t.Done,
                    due: t.DueDate,                                // DateTime?
                    assigneeId: t.Assignees.FirstOrDefault()?.UserId // int?
                ))
                .ToList();


            var memberIds = Participants
                .Where(p => p.UserId.HasValue)
                .Select(p => p.UserId!.Value)
                .Distinct()
                .ToList();

            // Kutsu palvelua / Tallenna
            var id = await _projects.CreateProjectAsync(name, desc, start, end, taskDtos, memberIds);

            await DisplayAlert("OK", $"Projekti luotu (ID {id}).", "Sulje");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", ex.Message, "Sulje");
        }
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;

        await btn.ScaleTo(0.9, 80, Easing.CubicOut);
        await btn.ScaleTo(1.0, 80, Easing.CubicIn);

        await Shell.Current.GoToAsync("..");
    }
    
}

// ------- rivamallit -------
public class TaskRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public long? Id { get; set; }
    public string Title { get; set; } = "";
    public bool Done { get; set; }

    // Eräpäivä (oletus tänään)
    DateTime? _dueDate = DateTime.Today;
    public DateTime? DueDate { get => _dueDate; set { _dueDate = value; OnPropertyChanged(nameof(DueDate)); } }

    // Valitut vastuuhenkilöt
    public ObservableCollection<MemberRow> Assignees { get; } = new();

    // Per-tehtävä haku UI:ta varten
    public ObservableCollection<UserDto> FilteredUsers { get; } = new();
    bool _areUserSuggestionsVisible;
    public bool AreUserSuggestionsVisible { get => _areUserSuggestionsVisible; set { _areUserSuggestionsVisible = value; OnPropertyChanged(nameof(AreUserSuggestionsVisible)); } }
    public string SearchText { get; set; } = "";
}

public class MemberRow
{
    public long? Id { get; set; }       // mahdollinen projekti_osallistuja id muokkaussivulla
    public int? UserId { get; set; }    // user.user_id
    public string FullName { get; set; } = "";
    public string? Role { get; set; }
}

