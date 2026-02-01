using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
namespace Proyecto_A1;

public static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Usuarios").WithTags(nameof(Usuarios));

        group.MapGet("/", async (PagosMovilesContext db) =>
        {
            return await db.Usuarios.ToListAsync();
        })
        .WithName("GetAllUsuarios")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Usuarios>, NotFound>> (int idusuario, PagosMovilesContext db) =>
        {
            return await db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(model => model.IdUsuario == idusuario)
                is Usuarios model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetUsuariosById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int idusuario, Usuarios usuarios, PagosMovilesContext db) =>
        {
            var affected = await db.Usuarios
                .Where(model => model.IdUsuario == idusuario)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.IdUsuario, usuarios.IdUsuario)
                    .SetProperty(m => m.NombreCompleto, usuarios.NombreCompleto)
                    .SetProperty(m => m.TipoIdentificacion, usuarios.TipoIdentificacion)
                    .SetProperty(m => m.Identificacion, usuarios.Identificacion)
                    .SetProperty(m => m.Email, usuarios.Email)
                    .SetProperty(m => m.Telefono, usuarios.Telefono)
                    .SetProperty(m => m.Usuario, usuarios.Usuario)
                    .SetProperty(m => m.Contraseña, usuarios.Contraseña)
                    .SetProperty(m => m.IdEstado, usuarios.IdEstado)
                    .SetProperty(m => m.IdRol, usuarios.IdRol)
                    .SetProperty(m => m.FechaCreacion, usuarios.FechaCreacion)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateUsuarios")
        .WithOpenApi();

        group.MapPost("/", async (Usuarios usuarios, PagosMovilesContext db) =>
        {
            db.Usuarios.Add(usuarios);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Usuarios/{usuarios.IdUsuario}",usuarios);
        })
        .WithName("CreateUsuarios")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int idusuario, PagosMovilesContext db) =>
        {
            var affected = await db.Usuarios
                .Where(model => model.IdUsuario == idusuario)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteUsuarios")
        .WithOpenApi();
    }
}
