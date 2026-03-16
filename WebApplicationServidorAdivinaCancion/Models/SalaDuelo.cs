namespace WebApplicationServidorAdivinaCancion.Models;

public class SalaDuelo
{
    public string SalaId { get; set; }
    public List<Player> Jugadores { get; set; } = new List<Player>();
    public bool JuegoIniciado { get; set; } = false;

    public List<Result> ListaCanciones { get; set; } = new List<Result>();
    public Result? CancionActual { get; set; }
    public int RondaActual { get; set; } = 0;

    public CancellationTokenSource? TokenCancelacionRonda { get; set; }
    public HashSet<string> JugadoresQueFallaronRonda { get; set; } = new HashSet<string>();
    public string? FotoArtista { get; set; }

    public Dictionary<string, DateTime> RespuestasCorrectasRonda { get; set; } = new Dictionary<string, DateTime>();

    public SalaDuelo(string id)
    {
        SalaId = id;
    }

    public bool AvanzarRonda()
    {
        if (RondaActual < ListaCanciones.Count)
        {
            CancionActual = ListaCanciones[RondaActual];
            RondaActual++;
            RespuestasCorrectasRonda.Clear();
            return true;
        }
        return false;
    }
}