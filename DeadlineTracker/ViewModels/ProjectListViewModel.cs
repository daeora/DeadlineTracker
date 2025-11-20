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
        // Projektit jaettu aktiivisiin ja valmiisiin
        public ObservableCollection<Project> ActiveProjects { get; set; } = new();
        public ObservableCollection<Project> CompletedProjects { get; set; } = new();

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

            // 1. Tyhjennetään listat
            if (force)
            {
                ActiveProjects.Clear();
                CompletedProjects.Clear();
            }

            // 2. Poista projektit joita ei enää ole
            var allExisting = ActiveProjects.Concat(CompletedProjects).ToList();
            foreach (var oldProj in allExisting)
            {
                if (!rows.Any(r => r.Id == oldProj.ProjektiId) && !oldProj.IsAddButton)
                {
                    ActiveProjects.Remove(oldProj);
                    CompletedProjects.Remove(oldProj);
                }
            }

            // 3. Käydään läpi tietokannan projektit
            foreach (var r in rows)
            {
                // Etsi jo olemassaoleva projekti
                var existing = ActiveProjects.FirstOrDefault(p => p.ProjektiId == r.Id)
                               ?? CompletedProjects.FirstOrDefault(p => p.ProjektiId == r.Id);

                if (existing == null)
                {
                    // Luodaan uusi projekti
                    existing = new Project
                    {
                        ProjektiId = r.Id,
                        ProjektiNimi = r.Name,
                        Loppupvm = r.EndDate,
                        DoneCount = r.DoneCount,
                        TotalCount = r.TotalCount
                    };

                    existing.ReplaceOpenTasks(r.OpenTasks);
                }
                else
                {
                    // Päivitetään olemassa ehkä muutettu projekti
                    existing.ProjektiNimi = r.Name;
                    existing.Loppupvm = r.EndDate;
                    existing.DoneCount = r.DoneCount;
                    existing.TotalCount = r.TotalCount;
                    existing.ReplaceOpenTasks(r.OpenTasks);
                }

                existing.PaivitaValmiusJaNakyma();

                // 4. Siirrä oikeaan listaan (active/completed)
                ActiveProjects.Remove(existing);
                CompletedProjects.Remove(existing);

                if (existing.OnValmis)
                    CompletedProjects.Add(existing);
                else
                    ActiveProjects.Add(existing);
            }

            // 5. Varmista että Add Project -kortti on lopussa
            var addBtn = ActiveProjects.FirstOrDefault(p => p.IsAddButton);
            if (addBtn != null)
                ActiveProjects.Remove(addBtn);

            ActiveProjects.Add(new Project { IsAddButton = true });
        }
        public async Task CompleteTaskAsync(Tehtava task)
        {
            if (task == null) return;

            if (await _service.MarkTaskDoneAsync(task.TehtavaId))
            {
                var proj = ActiveProjects.FirstOrDefault(p => p.ProjektiId == task.ProjektiId);
                if (proj != null)
                {
                    var item = proj.Tehtavat.FirstOrDefault(t => t.TehtavaId == task.TehtavaId);
                    if (item != null)
                        proj.Tehtavat.Remove(item);

                    proj.DoneCount++;
                    proj.PaivitaValmiusJaNakyma();

                    // SIIRTÄMINEN LISTALTA TOISELLE
                    if (proj.OnValmis)
                    {
                        ActiveProjects.Remove(proj);
                        CompletedProjects.Insert(0, proj); // näkyy ylimpänä
                    }
                }
            }
        }

        public void AddNewProject(Project newProject)
        {
            // UI päivitys tehdään täällä, jotta lisää nappi näkyy oikeassa paikassa
            // Poistetaan “Lisää projekti” -kortti hetkeksi
            var addBtn = ActiveProjects.FirstOrDefault(p => p.IsAddButton);
            if (addBtn != null)
            {
                ActiveProjects.Remove(addBtn);
            }

            // Lisää uusi projekti
            ActiveProjects.Add(newProject);

            // Lisää nappi takaisin loppuun
            if (addBtn != null)
            {
                ActiveProjects.Add(addBtn);
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
