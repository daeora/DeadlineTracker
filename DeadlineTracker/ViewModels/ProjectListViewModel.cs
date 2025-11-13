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
        public async Task LoadProjectsAsync(int userId)
        {
            var projects = await _service.GetProjectsForUserAsync(userId, includeCompletedTasks: true);

            // Jos ei ole vielä ladattu mitään, alustetaan lista
            if (Projects.Count == 0)
            {
                foreach (var p in projects)
                    Projects.Add(p);

                Projects.Add(new Project { IsAddButton = true });
            }
            else
            {
                // Päivitä olemassa oleva kokoelma ilman että UI:n bindingit katkeavat
                var addBtn = Projects.FirstOrDefault(p => p.IsAddButton);
                if (addBtn != null)
                    Projects.Remove(addBtn);

                // Lisää uudet vain jos eivät jo ole listassa
                foreach (var p in projects)
                {
                    if (!Projects.Any(existing => existing.ProjektiId == p.ProjektiId))
                        Projects.Add(p);
                }

                // Lisää "lisää projekti" -nappi takaisin loppuun
                Projects.Add(new Project { IsAddButton = true });
            }
        }
        public async Task CompleteTaskAsync(Tehtava task)
        {
            if (task == null)
                return;

            bool success = await _service.MarkTaskDoneAsync(task.TehtavaId);
            if (success)
            {
                task.OnValmis = true;
                var project = Projects.FirstOrDefault(p => p.ProjektiId == task.ProjektiId);
                project?.PaivitaValmiusJaNakyma();
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
