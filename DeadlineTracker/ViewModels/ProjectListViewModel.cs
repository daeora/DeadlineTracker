using DeadlineTracker.Models;
using DeadlineTracker.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DeadlineTracker.ViewModels
{
    public partial class ProjectListViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Project> Projects { get; set; } = new();

        private readonly ProjectService _service;

        public ProjectListViewModel()
        {
            _service = new ProjectService(AuthService.ConnectionString);
        }

        // Näytä omat projektit / näytä kaikki MVVM
        public bool ShowAll { get => _showAll; set { _showAll = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToggleAllText)); } }
        private bool _showAll;

        public string ToggleAllText => ShowAll ? "Näytä omat" : "Näytä kaikki";

        public ICommand ToggleAllCommand => new Command(async () =>
        {
            ShowAll = !ShowAll;
            await LoadProjectsAsync(Session.CurrentUserId, force: true, all: ShowAll);
        });

        public async Task LoadProjectsAsync(int userId, bool force = false, bool all = false)
        {
            var rows = await _service.GetDashboardProjectsAsync(all ? (int?)null : userId);

            var addBtn = Projects.FirstOrDefault(p => p.IsAddButton);
            if (force) Projects.Clear();
            if (addBtn != null) Projects.Remove(addBtn);

            // poista kadonneet
            var toRemove = Projects.Where(p => !p.IsAddButton && !rows.Any(x => x.Id == p.ProjektiId)).ToList();
            foreach (var r in toRemove) Projects.Remove(r);

            foreach (var r in rows)
            {
                var existing = Projects.FirstOrDefault(p => (long)p.ProjektiId == r.Id);
                if (existing == null)
                {
                    existing = new Project
                    {
                        ProjektiId = r.Id,
                        ProjektiNimi = r.Name,
                        Loppupvm = r.EndDate,
                        DoneCount = r.DoneCount,
                        TotalCount = r.TotalCount
                    };
                    existing.ReplaceOpenTasks(r.OpenTasks);
                    Projects.Add(existing);
                }
                else
                {
                    existing.ProjektiNimi = r.Name;
                    existing.Loppupvm = r.EndDate;
                    existing.DoneCount = r.DoneCount;
                    existing.TotalCount = r.TotalCount;
                    existing.ReplaceOpenTasks(r.OpenTasks);
                }

                existing.PaivitaValmiusJaNakyma();
            }

            Projects.Add(addBtn ?? new Project { IsAddButton = true });
        }
        public async Task CompleteTaskAsync(Tehtava task)
        {
            if (task == null) return;

            if (await _service.MarkTaskDoneAsync(task.TehtavaId))
            {
                // päivitä UI heti: poista kortin keskeneräisistä ja nosta 0/0
                var proj = Projects.FirstOrDefault(p => p.ProjektiId == task.ProjektiId);
                if (proj != null)
                {
                    var item = proj.Tehtavat.FirstOrDefault(t => t.TehtavaId == task.TehtavaId);
                    if (item != null) proj.Tehtavat.Remove(item); // poista kortista
                    proj.DoneCount++;                              // nosta 0/0
                    proj.PaivitaValmiusJaNakyma();
                }
            }
        }

        public void AddNewProject(Project newProject)
        {
            // UI päivitys tehdään täällä, jotta lisää nappi näkyy oikeassa paikassa
            // Poistetaan “Lisää projekti” -kortti hetkeksi
            var addBtn = Projects.FirstOrDefault(p => p.IsAddButton);
            if (addBtn != null)
            {
                Projects.Remove(addBtn);
            }

            // Lisää uusi projekti
            Projects.Add(newProject);

            // Lisää nappi takaisin loppuun
            if (addBtn != null)
            {
                Projects.Add(addBtn);
            }
            // Päivitä valmius ja varmista tehtävien binding
            newProject.PaivitaValmiusJaNakyma();
            foreach (var t in newProject.Tehtavat)
            {
                t.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Tehtava.OnValmis))
                        newProject.PaivitaValmiusJaNakyma();
                };
            }
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
