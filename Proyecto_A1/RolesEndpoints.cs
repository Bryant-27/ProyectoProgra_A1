using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Services.Interfaces;
using Entities;

namespace Proyecto_A1;

public static class RolesEndpoints
{
    public static void MapRolesEndpoints (this IEndpointRouteBuilder routes)
    {

        // Se cambia a RequireAuthorization para proteger las rutas y
        // requerir autenticación para acceder a ellas. Esto asegura
        // que solo los usuarios autenticados puedan interactuar con
        // los endpoints relacionados con TablaPantallas, mejorando
        // la seguridad de la aplicación.

        var group = routes.MapGroup("/rol").RequireAuthorization().WithTags(nameof(Roles));

        /* ===== METODOS GET ===== */

        group.MapGet("/", async (
            [FromServices] IBitacoraService bitacora,
            [FromServices] PagosMovilesContext db,
            HttpContext context) =>
        {
            
            var usuario = context.User.Identity?.Name ?? "Usuario Desconocido";

            var listaPantallas = await db.Roles.ToListAsync();

            await bitacora.RegistrarAccionBitacora(
                usuario: usuario,
                accion: "Consulta de roles",
                resultado: "Éxito",
                descripcion: $"El usuario {usuario} consultó la lista de roles."
            );

            return await db.Roles.ToListAsync();
        })
        .WithName("GetAllRoles")
        .WithOpenApi();

        // ===== METODOS GET POR ID =====

        group.MapGet("/{id}", async Task<Results<Ok<Roles>, NotFound>> (
            int idrol, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora) =>
        {

            var usuario = "Usuario desconocido"; // Aquí podrías obtener el usuario autenticado desde el contexto
            var rol = await db.Roles
               .AsNoTracking()
               .FirstOrDefaultAsync(model => model.IdRol== idrol);

            if (rol is null)
            {
                await bitacora.RegistrarAccionBitacora(
                    usuario,
                    "Obtener pantalla por ID",
                    "No encontrado",
                    $"No se encontró la pantalla con ID {idrol}."
                );

                return TypedResults.NotFound();
            }

            await bitacora.RegistrarAccionBitacora(
                usuario,
                "Obtener pantalla por ID",
                "Éxito",
                $"Se obtuvo la pantalla con ID {idrol}."
                );

            return TypedResults.Ok(rol);

        })
        .WithName("GetRolesById")
        .WithOpenApi();

        // ===== METODOS PUT =====

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (
            int idrol, 
            [FromBody] Roles roles, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora) =>
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

        group.MapPost("/", async (
            //[FromBody] Roles roles,
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            [FromBody] RolDTO rol) =>
        {

            if (rol.Pantallas == null || !rol.Pantallas.Any())
                return Results.BadRequest("El rol debe tener al menos una pantalla asignada.");

            var pv = await db.TablaPantallas
                .Where(p => rol.Pantallas.Contains(p.IdPantalla))
                .Select(p => p.IdPantalla)
                .ToListAsync();

            if (pv.Count != rol.Pantallas.Count)
                return Results.BadRequest("La pantalla asignada no existe.");

            var NewRol = new Roles
            {
                IdRol = rol.ID,
                Nombre = rol.Nombre,
                Descripcion = rol.Descripcion,

            };

            db.Roles.Add(NewRol);
            await db.SaveChangesAsync(); //Esto guarda el nuevo rol para obtener su ID 

            foreach (var idPantalla in rol.Pantallas)
            {
                db.RolPorPantalla.Add(new RolPorPantalla
                {
                    IdRol = NewRol.IdRol,
                    IdPantalla = idPantalla
                });
            }

            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Roles/{rol.ID}",NewRol);
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
