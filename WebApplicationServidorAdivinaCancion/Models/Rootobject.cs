using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WebApplicationServidorAdivinaCancion.Models
{
    public class Result : INotifyPropertyChanged
    {
        public string wrapperType { get; set; }
        public string kind { get; set; }
        public string artistName { get; set; }
        public long artistId { get; set; }
        public string trackName { get; set; }
        public string previewUrl { get; set; }
        public string artistViewUrl { get; set; }
        public string artworkUrl100 { get; set; }
        public bool isStreamable { get; set; }

        private bool _fueAdivinada;
        public bool FueAdivinada
        {
            get => _fueAdivinada;
            set { _fueAdivinada = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Rootobject
    {
        public int resultCount { get; set; }
        public List<Result> results { get; set; }
    }
}