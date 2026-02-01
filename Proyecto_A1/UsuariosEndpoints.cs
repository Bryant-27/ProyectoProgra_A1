using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
namespace Proyecto_A1;

public static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Usuarios").WithTags(nameof(Usuarios));

        // [FromServices] se agrega para inyectar el contexto de la base de datos
        // [FromServices] se utiliza para indicar que el PagosMovilesContext
        // no proviene de la solicitud HTTP (body, ruta o query),
        // sino que debe ser inyectado desde el contenedor de dependencias (DI).
        // Esto evita que ASP.NET Core intente inferirlo como un parámetro del body,
        // lo cual provocaría un error en tiempo de ejecución, especialmente en
        // endpoints GET donde no existe cuerpo en la petición.


        group.MapGet("/", async ([FromServices] PagosMovilesContext db) =>
        {
            return await db.Usuarios.ToListAsync();
        })
        .WithName("GetAllUsuarios")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Usuarios>, NotFound>> (int idusuario, [FromServices] PagosMovilesContext db) =>
        {
            return await db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(model => model.IdUsuario == idusuario)
                is Usuarios model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetUsuariosById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int idusuario, Usuarios usuarios, [FromServices] PagosMovilesContext db) =>
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

        group.MapPost("/", async (Usuarios usuarios, [FromServices] PagosMovilesContext db) =>
        {
            db.Usuarios.Add(usuarios);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Usuarios/{usuarios.IdUsuario}",usuarios);
        })
        .WithName("CreateUsuarios")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int idusuario, [FromServices] PagosMovilesContext db) =>
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
