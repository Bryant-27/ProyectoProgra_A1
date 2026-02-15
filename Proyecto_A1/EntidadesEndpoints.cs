using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
namespace Proyecto_A1;

public static class EntidadesEndpoints
{
    public static void MapEntidadesEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Entidades").WithTags(nameof(Entidades));

        // ===== METODOS GET =====

        group.MapGet("/", async ([FromServices] PagosMovilesContext db) =>
        {
            return await db.Entidades.ToListAsync();
        })
        .WithName("GetAllEntidades")
        .WithOpenApi();

        // ===== METODOS GET POR ID =====

        group.MapGet("/{id}", async Task<Results<Ok<Entidades>, NotFound>> (int identidad, [FromServices] PagosMovilesContext db) =>
        {
            return await db.Entidades.AsNoTracking()
                .FirstOrDefaultAsync(model => model.IdEntidad == identidad)
                is Entidades model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetEntidadesById")
        .WithOpenApi();

        // ===== METODOS PUT =====

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int identidad,[FromBody] Entidades entidades, [FromServices] PagosMovilesContext db) =>
        {
            var affected = await db.Entidades
                .Where(model => model.IdEntidad == identidad)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.IdEntidad, entidades.IdEntidad)
                    .SetProperty(m => m.NombreInstitucion, entidades.NombreInstitucion)
                    .SetProperty(m => m.IdEstado, entidades.IdEstado)
                    .SetProperty(m => m.FechaCreacion, entidades.FechaCreacion)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateEntidades")
        .WithOpenApi();

        // ===== METODOS POST =====

        group.MapPost("/", async ([FromBody] Entidades entidades, [FromServices] PagosMovilesContext db) =>
        {
            db.Entidades.Add(entidades);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Entidades/{entidades.IdEntidad}",entidades);
        })
        .WithName("CreateEntidades")
        .WithOpenApi();

        // ===== METODOS DELETE =====

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int identidad, [FromServices] PagosMovilesContext db) =>
        {
            var affected = await db.Entidades
                .Where(model => model.IdEntidad == identidad)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteEntidades")
        .WithOpenApi();
    }
}
