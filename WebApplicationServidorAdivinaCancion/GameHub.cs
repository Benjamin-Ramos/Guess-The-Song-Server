using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using WebApplicationServidorAdivinaCancion.Models;
using WpfAppClienteAdivinarCancion;

public class GameHub : Hub
{
    private readonly IHubContext<GameHub> _hubContext;

    public GameHub(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    private static ConcurrentDictionary<string, SalaDuelo> Salas = new ConcurrentDictionary<string, SalaDuelo>();

    public async Task UnirseASala(string salaId, string nombreJugador)
    {
        var sala = Salas.GetOrAdd(salaId, s => new SalaDuelo(s));

        if (sala.Jugadores.Any(p => p.Nombre.Equals(nombreJugador, StringComparison.OrdinalIgnoreCase)))
        {
            await Clients.Caller.SendAsync("ErrorDeConexion", "El nombre de usuario ya está en uso.");
            return;
        }

        var nuevoJugador = new Player
        {
            ConnectionId = Context.ConnectionId,
            Nombre = nombreJugador
        };
        sala.Jugadores.Add(nuevoJugador);

        await Groups.AddToGroupAsync(Context.ConnectionId, salaId);
        await Clients.Caller.SendAsync("LoginExitoso");

        if (sala.Jugadores.Count == 1)
        {
            await Clients.Caller.SendAsync("AsignarHost", true);
        }

        await NotificarActualizacionJugadores(salaId);
        await Clients.Group(salaId).SendAsync("RecibirMensaje", "Sistema", $"{nombreJugador} se ha unido a la sala.");
    }

    public async Task EnviarMensaje(string salaId, string usuario, string mensaje)
    {
        await Clients.Group(salaId).SendAsync("RecibirMensaje", usuario, mensaje);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var salaEntry in Salas)
        {
            var salaId = salaEntry.Key;
            var sala = salaEntry.Value;
            var jugador = sala.Jugadores.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

            if (jugador != null)
            {
                sala.Jugadores.Remove(jugador);
                await NotificarActualizacionJugadores(salaId);
                await Clients.Group(salaId).SendAsync("RecibirMensaje", "Sistema", $"{jugador.Nombre} ha salido.");

                if (sala.Jugadores.Count == 0) Salas.TryRemove(salaId, out _);
                else
                {
                    var nuevoHost = sala.Jugadores.First();
                    await Clients.Client(nuevoHost.ConnectionId).SendAsync("AsignarHost", true);
                }
                break;
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task IniciarPartida(string salaId, List<Result> cancionesCargadas, string artistaNombre)
    {
        if (Salas.TryGetValue(salaId, out var sala))
        {
            sala.ListaCanciones = cancionesCargadas.OrderBy(x => Guid.NewGuid()).ToList();
            sala.JuegoIniciado = true;
            sala.RondaActual = 0;

            string fotoFinal = "";
            try
            {
                var cancionSolista = cancionesCargadas.FirstOrDefault(r =>
                    r.artistName.Equals(artistaNombre, StringComparison.OrdinalIgnoreCase));

                string urlPerfilArtista = cancionSolista?.artistViewUrl ?? cancionesCargadas[0].artistViewUrl;

                if (!string.IsNullOrEmpty(urlPerfilArtista))
                {
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        var html = await client.GetStringAsync(urlPerfilArtista);

                        string patron = "<meta property=\"og:image\" content=\"([^\"]+)\"";
                        var match = Regex.Match(html, patron);

                        if (match.Success)
                        {
                            fotoFinal = match.Groups[1].Value;
                        }
                        else
                        {
                            fotoFinal = (cancionSolista ?? cancionesCargadas[0]).artworkUrl100.Replace("100x100", "600x600");
                        }
                    }
                }
            }
            catch
            {
                fotoFinal = cancionesCargadas[0].artworkUrl100.Replace("100x100", "600x600");
            }

            sala.FotoArtista = fotoFinal;

            await Clients.Group(salaId).SendAsync("CargarImagenArtista", fotoFinal);

            await Clients.Group(salaId).SendAsync("IniciarConteo");

            await Task.Delay(4000);

            await ProximaRonda(salaId);
        }
    }
    public async Task ReiniciarPuntos(string salaId)
    {
        if (Salas.TryGetValue(salaId, out var sala))
        {
            var esHost = sala.Jugadores.FirstOrDefault()?.ConnectionId == Context.ConnectionId;

            if (esHost)
            {
                foreach (var j in sala.Jugadores) j.PuntosTotales = 0;
                await NotificarActualizacionJugadores(salaId);
                await Clients.Group(salaId).SendAsync("RecibirMensaje", "SISTEMA", "Marcadores reiniciados.");
            }
        }
    }
    public async Task ProximaRonda(string salaId)
    {
        if (Salas.TryGetValue(salaId, out var sala))
        {
            sala.TokenCancelacionRonda?.Cancel();
            sala.TokenCancelacionRonda = new CancellationTokenSource();
            var token = sala.TokenCancelacionRonda.Token;

            if (sala.RondaActual < sala.ListaCanciones.Count)
            {
                var cancion = sala.ListaCanciones[sala.RondaActual];
                sala.CancionActual = cancion;
                sala.RespuestasCorrectasRonda.Clear();
                sala.JugadoresQueFallaronRonda.Clear();

                string nombreCorrecto = LimpiarTitulo(cancion.trackName);

                var poolNombresFalsos = sala.ListaCanciones
                    .Select(c => LimpiarTitulo(c.trackName))
                    .Where(n => !string.Equals(n, nombreCorrecto, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(x => Guid.NewGuid())
                    .Take(5)
                    .ToList();

                poolNombresFalsos.Add(nombreCorrecto);
                var opcionesFinales = poolNombresFalsos.OrderBy(x => Guid.NewGuid()).ToList();

                sala.RondaActual++;

                await _hubContext.Clients.Group(salaId).SendAsync("NuevaRonda", new NuevaRondaDto
                {
                    PreviewUrl = cancion.previewUrl,
                    Opciones = opcionesFinales,
                    NumeroRonda = sala.RondaActual,
                    TotalRondas = sala.ListaCanciones.Count
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(25000, token);

                        if (!token.IsCancellationRequested)
                        {
                            await _hubContext.Clients.Group(salaId).SendAsync("PararAudio");
                            string titulo = LimpiarTitulo(sala.CancionActual.trackName);
                            await _hubContext.Clients.Group(salaId).SendAsync("RondaGanada", "Nadie", titulo, sala.CancionActual.artworkUrl100);

                            await Task.Delay(4000);
                            await ProximaRonda(salaId);
                        }
                    }
                    catch (OperationCanceledException) {}
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error en bucle: {ex.Message}");
                    }
                });
            }
            else
            {
                var podio = sala.Jugadores
                    .Select(p => new JugadorDto { Nombre = p.Nombre, PuntosTotales = p.PuntosTotales })
                    .OrderByDescending(p => p.PuntosTotales)
                    .ToList();

                await _hubContext.Clients.Group(salaId).SendAsync("FinDelJuego", podio);
                sala.JuegoIniciado = false;
            }
        }
    }

    public async Task ValidarRespuesta(string salaId, string respuestaCliente)
    {
        if (Salas.TryGetValue(salaId, out var sala) && sala.CancionActual != null)
        {
            var connectionId = Context.ConnectionId;
            var jugador = sala.Jugadores.FirstOrDefault(p => p.ConnectionId == connectionId);

            if (jugador == null || sala.RespuestasCorrectasRonda.Any() || sala.JugadoresQueFallaronRonda.Contains(connectionId))
            {
                return;
            }

            string tituloCorrecto = LimpiarTitulo(sala.CancionActual.trackName);
            bool esCorrecto = tituloCorrecto.Equals(respuestaCliente, StringComparison.OrdinalIgnoreCase);

            var grupo = Clients.Group(salaId);

            if (esCorrecto)
            {
                sala.TokenCancelacionRonda?.Cancel();
                sala.RespuestasCorrectasRonda.Add(connectionId, DateTime.Now);
                jugador.PuntosTotales += 10;

                await grupo.SendAsync("PararAudio");
                await grupo.SendAsync("RondaGanada", jugador.Nombre, tituloCorrecto, sala.CancionActual.artworkUrl100);

                await NotificarActualizacionJugadores(salaId);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(4000);
                    await ProximaRonda(salaId);
                });
            }
            else
            {
                jugador.PuntosTotales -= 5;
                sala.JugadoresQueFallaronRonda.Add(connectionId);
                await Clients.Caller.SendAsync("RespuestaIncorrecta");
                await NotificarActualizacionJugadores(salaId);
            }
        }
    }

    private async Task NotificarActualizacionJugadores(string salaId)
    {
        if (Salas.TryGetValue(salaId, out var sala))
        {
            var listaDtos = sala.Jugadores.Select(p => new JugadorDto
            {
                ConnectionId = p.ConnectionId,
                Nombre = p.Nombre,
                PuntosTotales = p.PuntosTotales,
                EstaListo = p.EstaListo,
                EsHost = (sala.Jugadores.Count > 0 && sala.Jugadores.First() == p)
            }).ToList();

            await Clients.Group(salaId).SendAsync("ActualizarListaJugadores", listaDtos);
        }
    }

    private string LimpiarTitulo(string titulo)
    {
        if (string.IsNullOrEmpty(titulo)) return "";

        string limpio = Regex.Replace(titulo, @"\(.*?\)|\[.*?\]", "");

        return limpio.Trim();
    }
}