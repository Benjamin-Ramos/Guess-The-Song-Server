using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAppClienteAdivinarCancion
{
    public class NuevaRondaDto
    {
        public string PreviewUrl { get; set; }
        public List<string> Opciones { get; set; }
        public int NumeroRonda { get; set; }
        public int TotalRondas { get; set; }
    }
}
