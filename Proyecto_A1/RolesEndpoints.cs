using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
namespace Proyecto_A1;

public static class RolesEndpoints
{
    public static void MapRolesEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/rol").WithTags(nameof(Roles));

        // ===== METODOS GET =====

        group.MapGet("/", async ([FromServices] PagosMovilesContext db) =>
        {
            return await db.Roles.ToListAsync();
        })
        .WithName("GetAllRoles")
        .WithOpenApi();

        // ===== METODOS GET POR ID =====

        group.MapGet("/{id}", async Task<Results<Ok<Roles>, NotFound>> (int idrol, [FromServices] PagosMovilesContext db) =>
        {
            return await db.Roles.AsNoTracking()
                .FirstOrDefaultAsync(model => model.IdRol == idrol)
                is Roles model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetRolesById")
        .WithOpenApi();

        // ===== METODOS PUT =====

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int idrol, [FromBody] Roles roles, [FromServices] PagosMovilesContext db) =>
        {
            var affected = await db.Roles
                .Where(model => model.IdRol == idrol)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.IdRol, roles.IdRol)
                    .SetProperty(m => m.Nombre, roles.Nombre)
                    .SetProperty(m => m.Descripcion, roles.Descripcion)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateRoles")
        .WithOpenApi();

        // ===== METODOS POST =====

        group.MapPost("/", async ([FromBody] Roles roles, [FromServices] PagosMovilesContext db) =>
        {
            db.Roles.Add(roles);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Roles/{roles.IdRol}",roles);
        })
        .WithName("CreateRoles")
        .WithOpenApi();

        // ===== METODOS DELETE =====

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int idrol, [FromServices] PagosMovilesContext db) =>
        {
            var affected = await db.Roles
                .Where(model => model.IdRol == idrol)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteRoles")
        .WithOpenApi();
    }
}
