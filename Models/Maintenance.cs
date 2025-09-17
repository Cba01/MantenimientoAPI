using System;

namespace MantenimientoApi.Models
{
    public class Maintenance
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid EquipoId { get; set; }
        public string EquipoNombre { get; set; } = string.Empty; 
        public DateTime FechaMantenimiento { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}
