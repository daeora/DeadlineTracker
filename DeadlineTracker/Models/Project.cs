using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DeadlineTracker.Models
{
    public class Project : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public long ProjektiId { get; set; }
        public string? ProjektiNimi { get; set; }
        public string? KuvausTeksti { get; set; }
        public DateTime Alkupvm { get; set; }
        public DateTime Loppupvm { get; set; }
        public DateTime LuotuPvm { get; set; }
        public DateTime PaivitettyPvm { get; set; }
        public bool OnValmis => DoneCount == TotalCount && TotalCount > 0;

        public ObservableCollection<Tehtava> Tehtavat { get; set; } = new();

        // 0/0 -laskurit tulevat palvelusta
        public int DoneCount { get; set; }
        public int TotalCount { get; set; }
        public string ValmiusTeksti => $"{DoneCount}/{TotalCount}";

        // UI:ssä nappikortti
        public bool IsAddButton { get; set; } = false;

        // apu: korvaa kortin “keskeneräiset”
        public void ReplaceOpenTasks(System.Collections.Generic.IEnumerable<Tehtava> openTasks)
        {
            Tehtavat.Clear();
            foreach (var t in openTasks)
            {
                t.ProjektiId = (int)ProjektiId; // jos Tehtävässä on int
                Tehtavat.Add(t);
            }
            OnPropertyChanged(nameof(Tehtavat));
            OnPropertyChanged(nameof(ValmiusTeksti));
        }

        // kun halutaan päivittää näyttötekstejä manuaalisesti
        public void PaivitaValmiusJaNakyma()
        {
            OnPropertyChanged(nameof(ValmiusTeksti));
            OnPropertyChanged(nameof(Tehtavat));
        }
    }
}
