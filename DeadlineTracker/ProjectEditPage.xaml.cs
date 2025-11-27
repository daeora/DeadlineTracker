using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using DeadlineTracker.Services;

namespace DeadlineTracker;

public partial class ProjectEditPage : ContentPage, IQueryAttributable
{
    private readonly ProjectService _projects = new(AuthService.ConnectionString);
    private readonly UserService _users = new(AuthService.ConnectionString);

    private long _projectId;

    // Sivun tilat (UI-sidonnat)
    public ObservableCollection<TaskRow> Tasks { get; } = new();
    public ObservableCollection<MemberRow> Participants { get; } = new();

    // Yleis-haku osallistujille (kannasta)
    private List<UserDto> _allUsers = new();
    public ObservableCollection<UserDto> FilteredUsers { get; } = new();
    public bool AreUserSuggestionsVisible { get; set; }

    public ProjectEditPage()
    {
        InitializeComponent();
        BindingContext = this;

        // Päivitä tehtävien assignee-hakua, kun projektin osallistujat muuttuvat
        Participants.CollectionChanged += (_, __) =>
        {
            foreach (var tr in Tasks)
            {
                if (!string.IsNullOrWhiteSpace(tr.SearchText))
                {
                    TaskAssigneeSearch_TextChanged(
                        new Entry { BindingContext = tr, Text = tr.SearchText },
                        new TextChangedEventArgs(tr.SearchText, tr.SearchText));
                }
            }
        };
    }

    // Shell välittää ?id=123 tänne
    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var val) && long.TryParse(val?.ToString(), out var id))
        {
            _projectId = id;
            await LoadAsync();
        }
        else
        {
            await DisplayAlert("Virhe", "Projektin tunnusta ei saatu.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // hae käyttäjät ehdotushakua varten
        _allUsers = await _users.GetAllAsync();
        FilteredUsers.Clear();
        AreUserSuggestionsVisible = false;
        OnPropertyChanged(nameof(AreUserSuggestionsVisible));
    }

    private async Task LoadAsync()
    {
        try
        {
            // 1) Projektin perusdata
            var p = await _projects.GetProjectDetailAsync(_projectId);
            NameEntry.Text = p.Name;
            DescEditor.Text = p.Description ?? "";
            StartDatePicker.Date = p.StartDate.Date;
            EndDatePicker.Date = p.EndDate.Date;

            // 2) Osallistujat
            Participants.Clear();
            var users = await _projects.GetProjectParticipantsAsync(_projectId);
            foreach (var u in users)
                Participants.Add(new MemberRow { UserId = u.Id, FullName = u.Name });

            // 3) Tehtävät
            Tasks.Clear();
            var taskRows = await _projects.GetProjectTasksAsync(_projectId);
            foreach (var t in taskRows)
            {
                var row = new TaskRow
                {
                    Id = t.Id,
                    Title = t.Title,
                    Done = t.Done,
                    DueDate = t.DueDate
                };

                // tallennamme edelleen vain yhden assigneen – jos on, lisää riville
                if (t.AssigneeId.HasValue && !string.IsNullOrEmpty(t.AssigneeName))
                    row.Assignees.Add(new MemberRow { UserId = t.AssigneeId, FullName = t.AssigneeName });

                Tasks.Add(row);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", ex.Message, "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    // -------- TEHTÄVÄT: lisäys/poisto + deadline + assignee-haku --------

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
            var ok = await DisplayAlert("Poista tehtävä", "Poistetaanko?", "Poista", "Peruuta");
            if (ok) Tasks.Remove(row);
        }
    }

    /* Luonti sivulta jäämistöä, ei tarvita muokkaussivulla. Sallii vain tämän ja tulevat päivät tehtävän deadlineksi.
    
    private void TaskDueDate_DateSelected(object sender, DateChangedEventArgs e)
    {
        if (sender is DatePicker dp && dp.BindingContext is TaskRow tr)
        {
            if (tr.DueDate.HasValue && tr.DueDate.Value.Date < DateTime.Today)
                tr.DueDate = DateTime.Today;
        }
    } 
    */

    private void TaskAssigneeSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is TaskRow tr)
        {
            var q = (e.NewTextValue ?? "").Trim();
            tr.SearchText = q;

            tr.FilteredUsers.Clear();

            if (q.Length >= 1)
            {
                // Ehdotukset vain projektin osallistujista
                var pool = Participants
                    .Where(p => p.UserId.HasValue)
                    .Select(p => new UserDto { Id = p.UserId!.Value, Name = p.FullName })
                    .ToList();

                foreach (var u in pool.Where(u => u.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
                    tr.FilteredUsers.Add(u);

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
                if (!tr.Assignees.Any(a => a.UserId == u.Id))
                    tr.Assignees.Add(new MemberRow { UserId = u.Id, FullName = u.Name });

                // reset dropdown
                tr.FilteredUsers.Clear();
                tr.AreUserSuggestionsVisible = false;
                tr.SearchText = "";

                if (cv.Parent is Frame f && f.Parent is Grid g)
                {
                    var entry = g.Children.OfType<Entry>().FirstOrDefault();
                    if (entry != null) entry.Text = "";
                }
            }
            cv.SelectedItem = null;
        }
    }

    private void TaskRemoveAssignee_Clicked(object sender, EventArgs e)
    {
        if (sender is Element el && el.BindingContext is MemberRow mr)
        {
            // löydä rivin TaskRow
            var parent = el.Parent as Element;
            while (parent != null && parent.BindingContext is not TaskRow)
                parent = parent.Parent as Element;

            if (parent?.BindingContext is TaskRow tr)
                tr.Assignees.Remove(mr);
        }
    }

    // -------- OSALLISTUJAT: haku kannasta + lisäys/poisto --------

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

    private void ParticipantSearch_Unfocused(object sender, FocusEventArgs e)
    {
        AreUserSuggestionsVisible = false;
        OnPropertyChanged(nameof(AreUserSuggestionsVisible));
    }

    private async void UserSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is UserDto u)
        {
            var already = Participants.Any(p => p.UserId == u.Id ||
                                                string.Equals(p.FullName, u.Name, StringComparison.OrdinalIgnoreCase));
            if (already)
            {
                await DisplayAlert("Jo lisätty", $"{u.Name} on jo osallistujissa.", "OK");
            }
            else
            {
                Participants.Add(new MemberRow { UserId = u.Id, FullName = u.Name, Role = "" });
            }

            ((CollectionView)sender).SelectedItem = null;
            ParticipantSearchEntry.Text = "";
            AreUserSuggestionsVisible = false;
            OnPropertyChanged(nameof(AreUserSuggestionsVisible));
            ParticipantSearchEntry.Unfocus();
        }
    }

    private async void DeleteParticipant_Clicked(object sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is MemberRow row)
        {
            var ok = await DisplayAlert("Poista osallistuja",
                                        $"Poistetaanko {row.FullName}?",
                                        "Poista", "Peruuta");
            if (ok) Participants.Remove(row);
        }
    }

    // -------- TALLENNUS / POISTO / PERUUTA --------

    private async void SaveChanges_Clicked(object sender, EventArgs e)
    {
        try
        {
            var name = NameEntry.Text?.Trim() ?? "";
            var desc = DescEditor.Text?.Trim() ?? "";
            var start = StartDatePicker.Date;
            var end = EndDatePicker.Date;

            // assigneejen pitää olla osallistujissa
            var participantIds = Participants.Where(p => p.UserId.HasValue).Select(p => p.UserId!.Value).ToHashSet();
            foreach (var t in Tasks)
            {
                var aid = t.Assignees.FirstOrDefault()?.UserId;
                if (aid.HasValue && !participantIds.Contains(aid.Value))
                    t.Assignees.Clear();
            }

            var taskDtos = Tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .Select(t => (
                    id: t.Id,
                    title: t.Title.Trim(),
                    done: t.Done,
                    due: t.DueDate,
                    assigneeId: t.Assignees.FirstOrDefault()?.UserId
                ))
                .ToList();

            var memberIds = Participants.Where(p => p.UserId.HasValue)
                                        .Select(p => p.UserId!.Value)
                                        .Distinct()
                                        .ToList();

            await _projects.UpdateProjectAsync(_projectId, name, desc, start, end, taskDtos, memberIds);

            await DisplayAlert("Tallennettu", "Muutokset tallennettu.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", ex.Message, "OK");
        }
    }

    private async void DeleteProject_Clicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Poista projekti", "Poistetaanko projekti pysyvästi?", "Poista", "Peruuta");
        if (!ok) return;

        try
        {
            await _projects.DeleteProjectAsync(_projectId);
            await DisplayAlert("Poistettu", "Projekti poistettu.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", ex.Message, "OK");
        }
    }

    private async void Cancel_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
