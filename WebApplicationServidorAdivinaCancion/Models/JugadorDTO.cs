namespace WpfAppClienteAdivinarCancion
{
    public class JugadorDto
    {
        public string ConnectionId { get; set; }
        public string Nombre { get; set; }
        public int PuntosTotales { get; set; }
        public bool EstaListo { get; set; }
        public bool EsHost { get; set; }
    }
}