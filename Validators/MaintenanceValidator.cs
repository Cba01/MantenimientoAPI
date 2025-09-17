using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MantenimientoApi.Models;
using MantenimientoApi.Repositories;

namespace MantenimientoApi.Validators
{
    public static class EnhancedValidationResult
    {
        private static readonly string[] PalabrasProblema = 
        {
            "falla", "fallo", "problema", "avería", "averia", "error", 
            "defecto", "daño", "roto", "descompuesto", "mal funcionamiento"
        };

        public static (bool IsValid, List<string> Errors, List<string> Warnings) 
            ValidateCreateAdvanced(MaintenanceCreateDto dto, IMaintenanceRepository repo)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Validaciones básicas existentes
            ValidateBasicFields(dto, errors);
            
            //Control de duplicados
            ValidateDuplicates(dto, repo, errors);
            
            //Secuencia temporal
            ValidateTemporalSequence(dto, repo, warnings);
            
            //Descripción contextual
            ValidateContextualDescription(dto, errors);
            
            // Límites temporales
            ValidateTemporalLimits(dto, errors);
            
            // Formato de GUIDs
            ValidateGuidFormats(dto, errors);

            return (errors.Count == 0, errors, warnings);
        }

        private static void ValidateBasicFields(MaintenanceCreateDto dto, List<string> errors)
        {
            if (dto.EquipoId == Guid.Empty)
                errors.Add("EquipoId es obligatorio.");
            
            if (string.IsNullOrWhiteSpace(dto.EquipoNombre))
                errors.Add("EquipoNombre es obligatorio.");
            
            if (dto.UsuarioId == Guid.Empty)
                errors.Add("UsuarioId es obligatorio.");
                
            if (string.IsNullOrWhiteSpace(dto.UsuarioNombre))
                errors.Add("UsuarioNombre es obligatorio.");

            if (dto.FechaMantenimiento == default)
                errors.Add("FechaMantenimiento es obligatoria.");
            else if (dto.FechaMantenimiento > DateTime.UtcNow.AddMinutes(1))
                errors.Add("FechaMantenimiento no puede ser futura.");

            var tipo = dto.Tipo?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(tipo) || (tipo != "preventivo" && tipo != "correctivo"))
                errors.Add("Tipo debe ser 'preventivo' o 'correctivo'.");

            if (string.IsNullOrWhiteSpace(dto.Descripcion) || dto.Descripcion.Length < 10)
                errors.Add("Descripcion debe tener al menos 10 caracteres.");

            if (dto.Descripcion?.Length > 2000)
                errors.Add("Descripcion demasiado larga (máx 2000).");
        }

        //Control de duplicados
        private static void ValidateDuplicates(MaintenanceCreateDto dto, IMaintenanceRepository repo, List<string> errors)
        {
            var mantenimientosDelDia = repo.List()
                .Where(m => m.EquipoId == dto.EquipoId && 
                           m.FechaMantenimiento.Date == dto.FechaMantenimiento.Date);

            if (mantenimientosDelDia.Any())
            {
                var existente = mantenimientosDelDia.First();
                errors.Add($"Ya existe un mantenimiento {existente.Tipo} para este equipo en la fecha {dto.FechaMantenimiento:yyyy-MM-dd}. " +
                          $"ID del mantenimiento existente: {existente.Id}");
            }
        }

        //Secuencia temporal
        private static void ValidateTemporalSequence(MaintenanceCreateDto dto, IMaintenanceRepository repo, List<string> warnings)
        {
            if (dto.Tipo?.ToLowerInvariant() == "correctivo")
            {
                var ultimoPreventivo = repo.List()
                    .Where(m => m.EquipoId == dto.EquipoId && 
                               m.Tipo == "preventivo" && 
                               m.FechaMantenimiento < dto.FechaMantenimiento)
                    .OrderByDescending(m => m.FechaMantenimiento)
                    .FirstOrDefault();

                if (ultimoPreventivo != null)
                {
                    var diasTranscurridos = (dto.FechaMantenimiento - ultimoPreventivo.FechaMantenimiento).Days;
                    if (diasTranscurridos < 7)
                    {
                        warnings.Add($"ADVERTENCIA: Mantenimiento correctivo muy pronto después del preventivo " +
                                   $"(solo {diasTranscurridos} días). Considere revisar la efectividad del mantenimiento preventivo.");
                    }
                }
            }
        }

        //Descripción contextual
        private static void ValidateContextualDescription(MaintenanceCreateDto dto, List<string> errors)
        {
            if (dto.Tipo?.ToLowerInvariant() == "correctivo")
            {
                var descripcionLower = dto.Descripcion?.ToLowerInvariant() ?? "";
                bool contieneProblema = PalabrasProblema.Any(palabra => descripcionLower.Contains(palabra));

                if (!contieneProblema)
                {
                    errors.Add("Los mantenimientos correctivos deben especificar la naturaleza del problema. " +
                              "Incluya palabras como: falla, problema, avería, error, defecto, etc.");
                }
            }

            // descripción muy genérica
            var descripcionesGenericas = new[] { 
                "mantenimiento general", "revision general", "mantenimiento rutinario", 
                "revision rutinaria", "mantenimiento normal" 
            };

            if (descripcionesGenericas.Any(generica => 
                dto.Descripcion?.ToLowerInvariant().Contains(generica) == true))
            {
                errors.Add("La descripción es demasiado genérica. Proporcione detalles específicos de las tareas realizadas.");
            }
        }

        // Límites temporales
        private static void ValidateTemporalLimits(MaintenanceCreateDto dto, List<string> errors)
        {
            var hace30Dias = DateTime.UtcNow.AddDays(-30);
            if (dto.FechaMantenimiento < hace30Dias)
            {
                errors.Add($"No se pueden registrar mantenimientos con más de 30 días de antigüedad. " +
                          $"Fecha mínima permitida: {hace30Dias:yyyy-MM-dd}");
            }
        }

        // Formato de GUIDs
        private static void ValidateGuidFormats(MaintenanceCreateDto dto, List<string> errors)
        {
            // Validar que los GUIDs no sean el valor por defecto
            if (dto.EquipoId == Guid.Empty)
                errors.Add("EquipoId no puede ser un GUID vacío.");

            if (dto.UsuarioId == Guid.Empty)
                errors.Add("UsuarioId no puede ser un GUID vacío.");

            // Validación GUIDs secuenciales 
            var equipoBytes = dto.EquipoId.ToByteArray();
            var usuarioBytes = dto.UsuarioId.ToByteArray();

            if (IsSequentialGuid(equipoBytes))
                errors.Add("EquipoId parece ser un GUID de prueba. Use GUIDs reales en producción.");

            if (IsSequentialGuid(usuarioBytes))
                errors.Add("UsuarioId parece ser un GUID de prueba. Use GUIDs reales en producción.");
        }

        private static bool IsSequentialGuid(byte[] guidBytes)
        {
            // Detecta GUIDs como 00000000-0000-0000-0000-000000000001
            int zerosCount = guidBytes.Take(15).Count(b => b == 0);
            return zerosCount >= 14;
        }
    }
}