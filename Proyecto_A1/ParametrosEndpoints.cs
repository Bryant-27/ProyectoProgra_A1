using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
namespace Proyecto_A1;

public static class ParametrosEndpoints
{
    public static void MapParametrosEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/parametro").WithTags(nameof(Parametros));

        // ===== METODOS GET =====

        group.MapGet("/", async ([FromServices] PagosMovilesContext db) =>
        {
            return await db.Parametros.ToListAsync();
        })
        .WithName("GetAllParametros")
        .WithOpenApi();

        // ===== METODOS GET POR ID =====

        group.MapGet("/{id}", async Task<Results<Ok<Parametros>, NotFound>> (string idparametro, [FromServices] PagosMovilesContext db) =>
        {
            return await db.Parametros.AsNoTracking()
                .FirstOrDefaultAsync(model => model.IdParametro == idparametro)
                is Parametros model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetParametrosById")
        .WithOpenApi();


        // ===== METODOS PUT =====

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (string idparametro,[FromBody] Parametros parametros, [FromServices] PagosMovilesContext db) =>
        {
            var affected = await db.Parametros
                .Where(model => model.IdParametro == idparametro)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.IdParametro, parametros.IdParametro)
                    .SetProperty(m => m.Valor, parametros.Valor)
                    .SetProperty(m => m.IdEstado, parametros.IdEstado)
                    .SetProperty(m => m.FechaCreacion, parametros.FechaCreacion)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateParametros")
        .WithOpenApi();

        // ===== METODOS POST =====

        group.MapPost("/", async ([FromBody] Parametros parametros, [FromServices] PagosMovilesContext db) =>
        {
            db.Parametros.Add(parametros);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Parametros/{parametros.IdParametro}",parametros);
        })
        .WithName("CreateParametros")
        .WithOpenApi();

        // ===== METODOS DELETE =====

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (string idparametro, [FromServices] PagosMovilesContext db) =>
        {
            var affected = await db.Parametros
                .Where(model => model.IdParametro == idparametro)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteParametros")
        .WithOpenApi();
    }
}
