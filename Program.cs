using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using MantenimientoApi.Models;
using MantenimientoApi.Repositories;
using MantenimientoApi.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IMaintenanceRepository, InMemoryMaintenanceRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/mantenimientos", (MaintenanceCreateDto dto, IMaintenanceRepository repo) =>
{
    var (IsValid, Errors, Warnings) = EnhancedValidationResult.ValidateCreateAdvanced(dto, repo);

    if (!IsValid)
    {
        var errorDict = Errors
            .Select((error, index) => new { Key = $"error{index}", Value = new[] { error } })
            .ToDictionary(x => x.Key, x => x.Value);

        return Results.ValidationProblem(errorDict);
    }

    var m = new Maintenance
    {
        EquipoId = dto.EquipoId,
        EquipoNombre = dto.EquipoNombre,
        FechaMantenimiento = dto.FechaMantenimiento,
        Tipo = dto.Tipo.Trim().ToLowerInvariant(),
        Descripcion = dto.Descripcion,
        UsuarioId = dto.UsuarioId,
        UsuarioNombre = dto.UsuarioNombre
    };

    if (Warnings.Any())
    {
        Console.WriteLine($"Advertencias: {string.Join(", ", Warnings)}");
    }

    var saved = repo.Save(m);

    var response = new
    {
        mantenimiento = saved,
        advertencias = Warnings
    };

    return Results.Created($"/api/mantenimientos/{saved.Id}", response);
});

app.MapGet("/api/mantenimientos", (IMaintenanceRepository repo) =>
{
    var mantenimientos = repo.List();
    var count = mantenimientos.Count();

    return Results.Ok(mantenimientos);
});

app.MapGet("/api/mantenimientos/{id:guid}", (Guid id, IMaintenanceRepository repo) =>
{

    var m = repo.GetById(id);

    if (m == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(m);
});


app.Run();