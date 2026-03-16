namespace WebApplicationServidorAdivinaCancion.Models
{
    public class Player
    {
        public string ConnectionId { get; set; }
        public string Nombre { get; set; }
        public int PuntosTotales { get; set; }
        public bool EstaListo { get; set; }
        public long UltimoTiempoRespuesta { get; set; }
    }
}