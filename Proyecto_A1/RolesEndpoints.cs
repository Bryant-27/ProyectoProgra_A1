using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Services.Interfaces;
using Entities;
using Azure.Core;
using Proyecto_A1.Helper;

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
                    Pantallas = r.RolPorPantalla.Select(rp => rp.IdPantalla).ToList()
                })
                .FirstOrDefaultAsync();

            if (rol == null)
            {
                await bitacora.RegistrarAccionBitacora(
                    usuario,
                    "Obtener rol",
                    "No encontrado",
                    $"Rol {id} no existe"
                );

                return ApiResponse<RolDTO>.NotFound($"No existe el rol con ID {id}");
            }

            await bitacora.RegistrarAccionBitacora(
                usuario,
                "Obtener rol",
                "Éxito",
                $"Rol {id} consultado"
            );

            return ApiResponse<RolDTO>.Success(rol);
        })
            .WithName("GetRolesById")
            .WithOpenApi();


        // ===== METODOS PUT =====

        group.MapPut("/{id}", async (
         int id,
         [FromBody] RolDTO rol,
         [FromServices] PagosMovilesContext db,
         [FromServices] IBitacoraService bitacora,
         HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario Desconocido";

            var errores = ValidationHelper.ValidarModelo(rol);

            if (rol.Pantallas == null || !rol.Pantallas.Any())
                errores.Add("Debe asignar al menos una pantalla.");

            if (errores.Any())
                return ApiResponse<RolDTO>.Error(errores);

            var rolDb = await db.Roles
                .Include(r => r.RolPorPantalla)
                .FirstOrDefaultAsync(r => r.IdRol == id);

            if (rolDb == null)
                return ApiResponse<RolDTO>.NotFound($"No existe el rol con ID {id}");

            var pantallasValidas = await db.TablaPantallas
                .Where(p => rol.Pantallas.Contains(p.IdPantalla))
                .Select(p => p.IdPantalla)
                .ToListAsync();

            if (pantallasValidas.Count != rol.Pantallas.Count)
                return ApiResponse<RolDTO>.Error(new() { "Una o más pantallas no existen." });

            rolDb.Nombre = rol.Nombre;
            rolDb.Descripcion = rol.Descripcion;

            db.RolPorPantalla.RemoveRange(rolDb.RolPorPantalla);

            var nuevasRelaciones = rol.Pantallas.Select(idPantalla => new RolPorPantalla
            {
                IdRol = rolDb.IdRol,
                IdPantalla = idPantalla
            });

            db.RolPorPantalla.AddRange(nuevasRelaciones);

            await db.SaveChangesAsync();

            await bitacora.RegistrarAccionBitacora(
                usuario,
                "Actualizar rol",
                "Éxito",
                $"Rol {id} actualizado"
            );

            return ApiResponse<RolDTO>.Success(rol, "Rol actualizado correctamente");
        })
        .WithName("UpdateRoles")
        .WithOpenApi();

        // ===== METODOS POST =====

        group.MapPost("/", async (
            [FromServices] PagosMovilesContext db, 
            [FromServices] IBitacoraService bitacora, 
            [FromBody] RolDTO rol, HttpContext context) => 
        {
            var usuario = context.User.Identity?.Name ?? "Usuario Desconocido"; 
            if (rol.Pantallas == null || !rol.Pantallas.Any()) 
                return Results.BadRequest("El rol debe tener al menos una pantalla asignada."); 
            
            var pv = await db.TablaPantallas.Where(p => rol.Pantallas.Contains(p.IdPantalla)).Select(p => p.IdPantalla).ToListAsync(); 
            
            if (pv.Count != rol.Pantallas.Count) 
                return Results.BadRequest("La pantalla asignada no existe."); 
            var NewRol = new Roles { IdRol = rol.IdRol, Nombre = rol.Nombre, Descripcion = rol.Descripcion }; 
            
            db.Roles.Add(NewRol); await db.SaveChangesAsync(); //Esto guarda el nuevo rol para obtener su ID
            
            var relaciones = rol.Pantallas.Select(idPantalla => new RolPorPantalla 
            { IdRol = NewRol.IdRol, IdPantalla = idPantalla });
            
            db.RolPorPantalla.AddRange(relaciones); 
            
            await db.SaveChangesAsync(); 
            
            foreach (var idPantalla in rol.Pantallas) 
            { 
                db.RolPorPantalla.Add(new RolPorPantalla { IdRol = NewRol.IdRol, IdPantalla = idPantalla }); 
            } 
            await db.SaveChangesAsync(); 

            var respuesta = new RolDTO { IdRol = NewRol.IdRol, Nombre = NewRol.Nombre, Descripcion = NewRol.Descripcion, Pantallas = rol.Pantallas };
            
            await bitacora.RegistrarAccionBitacora( usuario, "Crear nuevo rol", "Éxito", $"Se creó un nuevo rol con ID {NewRol.IdRol}." ); 
            
            return TypedResults.Created($"/api/Roles/{rol.IdRol}",respuesta); 
        }) 
        .WithName("CreateRoles")
        .WithOpenApi();

            // ===== METODOS DELETE =====

            group.MapDelete("/{id}", async (
             int id,
             [FromServices] PagosMovilesContext db,
             [FromServices] IBitacoraService bitacora,
             HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario Desconocido";

            var rol = await db.Roles
                .Include(r => r.RolPorPantalla)
                .FirstOrDefaultAsync(r => r.IdRol == id);

            if (rol == null)
                return ApiResponse<string>.NotFound($"No existe el rol con ID {id}");

            if (rol.RolPorPantalla.Any())
                db.RolPorPantalla.RemoveRange(rol.RolPorPantalla);

            db.Roles.Remove(rol);
            await db.SaveChangesAsync();

            await bitacora.RegistrarAccionBitacora(
                usuario,
                "Eliminar rol",
                "Éxito",
                $"Rol {id} eliminado"
            );

            return ApiResponse<string>.Success($"Rol {id} eliminado");

        })
        .WithName("DeleteRoles")
        .WithOpenApi();
    }
}
