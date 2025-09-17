using System;

namespace MantenimientoApi.Models
{
    public class MaintenanceCreateDto
    {
        public Guid EquipoId { get; set; }
        public string EquipoNombre { get; set; } = string.Empty;
        public DateTime FechaMantenimiento { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
    }
}
