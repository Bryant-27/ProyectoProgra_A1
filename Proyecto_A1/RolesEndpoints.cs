using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Services.Interfaces;
using Entities;
using Azure.Core;

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

            var roles = await db.Roles
               .Select(r => new RolDTO
               {
                   IdRol = r.IdRol,
                   Nombre = r.Nombre,
                   Descripcion = r.Descripcion,
                   Pantallas = r.RolPorPantalla
                       .Select(rp => rp.IdPantalla)
                       .ToList()
               })
               .ToListAsync();

            await bitacora.RegistrarAccionBitacora(
               usuario: usuario,
               accion: "Consulta de roles",
               resultado: "Éxito",
               descripcion: $"El usuario {usuario} consultó la lista de roles."
           );

            return Results.Ok(roles);

            //return await db.Roles.ToListAsync();
        })
        .WithName("GetAllRoles")
        .WithOpenApi();

        // ===== METODOS GET POR ID =====

        group.MapGet("/{id}", async (
            int id,
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            HttpContext context) =>
            {
                var usuario = context.User.Identity?.Name ?? "Usuario Desconocido";

                var rol = await db.Roles
                    .Where(r => r.IdRol == id)
                    .Select(r => new RolDTO
                    {
                        IdRol = r.IdRol,
                        Nombre = r.Nombre,
                        Descripcion = r.Descripcion,
                        Pantallas = r.RolPorPantalla
                            .Select(rp => rp.IdPantalla)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (rol is null)
                {
                    await bitacora.RegistrarAccionBitacora(
                        usuario,
                        "Obtener rol por ID",
                        "No encontrado",
                        $"No se encontró el rol con ID {id}."
                    );

                    return Results.NotFound();
                }

                await bitacora.RegistrarAccionBitacora(
                    usuario,
                    "Obtener rol por ID",
                    "Éxito",
                    $"Se obtuvo el rol con ID {id}."
                );

                return Results.Ok(rol);
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
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            [FromBody] RolDTO rol,
            HttpContext context) =>
        {

            var usuario = context.User.Identity?.Name ?? "Usuario Desconocido";

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
                IdRol = rol.IdRol,
                Nombre = rol.Nombre,
                Descripcion = rol.Descripcion
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

            var respuesta = new RolDTO
            {
                IdRol = NewRol.IdRol,
                Nombre = NewRol.Nombre,
                Descripcion = NewRol.Descripcion,
                Pantallas = rol.Pantallas
            };

            await bitacora.RegistrarAccionBitacora(
               usuario, 
               "Crear nuevo rol",
               "Éxito",
               $"Se creó un nuevo rol con ID {NewRol.IdRol}."
           );

            return TypedResults.Created($"/api/Roles/{rol.IdRol}",respuesta);
        })
        .WithName("CreateRoles")
        .WithOpenApi();

        // ===== METODOS DELETE =====

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (
            int idrol, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora) =>

        {

            var affected = await db.Roles
                .Where(model => model.IdRol == idrol)
                .ExecuteDeleteAsync();
            
            if (affected == 1)
            {
                await bitacora.RegistrarAccionBitacora(
                  "Sistema",
                  "Eliminar Usuario",
                  "Exitoso",
                  $"Rol {idrol} eliminado",
                  "UsuariosEndpoint - DELETE"
              );

                return TypedResults.Ok();
            }

            await bitacora.RegistrarAccionBitacora(
                  "Sistema",
                  "Eliminar Usuario",
                  "No encontrado",
                  $"Rol {idrol} no encontrado para eliminación",
                  "UsuariosEndpoint - DELETE"
              );

            return TypedResults.NotFound();

        })
        .WithName("DeleteRoles")
        .WithOpenApi();
    }
}
