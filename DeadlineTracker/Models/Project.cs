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
        public int ProjektiId { get; set; }
        public string? ProjektiNimi { get; set; }
        public string? KuvausTeksti { get; set; }
        public DateTime Alkupvm { get; set; }
        public DateTime Loppupvm { get; set; }
        public DateTime LuotuPvm { get; set; }
        public DateTime PaivitettyPvm { get; set; }

        public ObservableCollection<Tehtava> Tehtavat { get; set; } = new();

        //lisää projekti nappulaa varten
        public bool IsAddButton { get; set; } = false;

        //Valmiusaste tekstinä, x/x
        public string ValmiusTeksti => $"{Tehtavat.Count(t => t.OnValmis)}/{Tehtavat.Count}";

        //Vain kortilla näytettävät (keskeneröiset) tehtävät
        public IEnumerable<Tehtava> NakymanTehtavat => Tehtavat.Where(t => !t.OnValmis);
        public void PaivitaValmiusJaNakyma()
        {
            OnPropertyChanged(nameof(ValmiusTeksti));
            OnPropertyChanged(nameof(NakymanTehtavat));
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
