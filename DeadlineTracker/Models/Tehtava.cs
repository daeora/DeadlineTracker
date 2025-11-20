using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DeadlineTracker.Models
{
    public partial class Tehtava : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int TehtavaId { get; set; }
        public int ProjektiId { get; set; }
        public string? TehtavaNimi { get; set; }
        public string? TehtavaKuvaus { get; set; }

        private bool _onValmis;
        public bool OnValmis
        {
            get => _onValmis;
            set
            {
                if (_onValmis != value)
                {
                    _onValmis = value;
                    OnPropertyChanged(nameof(OnValmis));

                    // Ilmoitetaan projektille, että valmius muuttui
                    ProjektiViite?.PaivitaValmiusJaNakyma();
                }
            }
        }

        public DateTime LuotuPvm { get; set; }
        public DateTime Erapaiva { get; set; }

        //Mihin projetkiin tehtävä kuuluu
        public Project? ProjektiViite { get; set; }

        // Ilmoitetaan UI:lle että tehtävän tila on muuttunut
        public void ToggleValmis()
        {
            OnValmis = !OnValmis;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OnValmis)));
        }
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
