using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Services;
using Proyecto_A1.Helper;
namespace Proyecto_A1;

public static class EntidadesEndpoints
{
    public static void MapEntidadesEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/entidad").WithTags(nameof(Entidades));

        // ===== METODOS GET =====

        group.MapGet("/", async (
            [FromServices] IBitacoraService bitacora,
            [FromServices] PagosMovilesContext db,
            HttpContext context) =>
        {

            var usuario = context.User.Identity?.Name ?? "Usuario no autenticado";

            var lista = await db.Entidades.ToListAsync();

            await bitacora.RegistrarAsync(
                usuario,
                accion: "Obtener todas las Entidades Bancarias",
                resultado: "Éxito",
                descripcion: $"Se obtuvieron {lista.Count} entidades."
            );

            return await db.Entidades.ToListAsync();
        })
        .WithName("GetAllEntidades")
        .WithOpenApi();

        // ===== METODOS GET POR ID =====

        group.MapGet("/{id}", async Task<Results<Ok<Entidades>, NotFound>> (
            string identidad, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora) =>
        {

            var usuario = "Usuario no autenticado";

            var model = await db.Entidades
                .AsNoTracking()
                .FirstOrDefaultAsync(model => model.IdEntidad == identidad);

            if (model is null)
            {
                await bitacora.RegistrarAsync(
                    usuario,
                    accion: "Obtener entidad por ID",
                    resultado: "No encontrado",
                    descripcion: $"No se encontró la entidad con ID: {identidad}."
                );
                return TypedResults.NotFound();
            }

            await bitacora.RegistrarAsync(
                    usuario,
                    accion: "Obtener entidad por ID",
                    resultado: "Éxito",
                    descripcion: $"Se obtuvo la entidad con ID: {identidad}."
                );

            return TypedResults.Ok(model);
            
        })
        .WithName("GetEntidadesById")
        .WithOpenApi();

        // ===== METODOS PUT =====

        group.MapPut("/{id}", async (
         string identidad,
         [FromBody] Entidades entidades,
         [FromServices] PagosMovilesContext db,
         [FromServices] IBitacoraService bitacora,
         HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario no autenticado";

            var errores = ValidationHelper.ValidarModelo(entidades);

            if (ValidationHelper.EsVacio(identidad))
                errores.Add("El ID de la ruta es obligatorio.");

            if (errores.Any())
            {
                await bitacora.RegistrarAsync(
                    usuario,
                    "Actualizar entidad",
                    "Error",
                    "Error de validación al actualizar entidad."
                );

                return ApiResponse<object>.Error(errores);
            }

            var affected = await db.Entidades
                .Where(model => model.IdEntidad == identidad)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.NombreInstitucion, entidades.NombreInstitucion)
                    .SetProperty(m => m.IdEstado, entidades.IdEstado)
                    .SetProperty(m => m.FechaCreacion, entidades.FechaCreacion)
                );

            if (affected == 0)
            {
                await bitacora.RegistrarAsync(
                    usuario,
                    "Actualizar entidad",
                    "No encontrado",
                    $"No existe entidad con ID: {identidad}"
                );

                return ApiResponse<object>.NotFound("Entidad no encontrada");
            }

            await bitacora.RegistrarAsync(
                usuario,
                "Actualizar entidad",
                "Éxito",
                $"Se actualizó la entidad con ID: {identidad}"
            );

            return ApiResponse<object>.Success(null, "Entidad actualizada correctamente");
        })
        .WithName("UpdateEntidades")
        .WithOpenApi();


        // ===== METODOS POST =====

        group.MapPost("/", async (
            [FromBody] Entidades entidades, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            HttpContext context) =>
        {

            var usuario = context.User.Identity?.Name ?? "Usuario no autenticado";

            var errores = ValidationHelper.ValidarModelo(entidades);

            if (ValidationHelper.EsVacio(entidades.IdEntidad))
                errores.Add("El ID no puede estar vacío.");

            if (errores.Any())
            {
                await bitacora.RegistrarAsync(
                    usuario,
                    "Crear entidad",
                    "Error",
                    "Error de validación al crear entidad."
                );

                return ApiResponse<Entidades>.Error(errores);
            }

            db.Entidades.Add(entidades);
            await db.SaveChangesAsync();

            await bitacora.RegistrarAsync(
                usuario,
                accion: "Crear entidad",
                resultado: "Éxito",
                descripcion: $"Se creó la entidad con ID: {entidades.IdEntidad}."
            );

            return ApiResponse<Entidades>.Success(entidades, "Entidad creada correctamente");
        })
        .WithName("CreateEntidades")
        .WithOpenApi();

        // ===== METODOS DELETE =====

        group.MapDelete("/{id}", async (
         string identidad,
         [FromServices] PagosMovilesContext db,
         [FromServices] IBitacoraService bitacora,
         HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario no autenticado";

            if (ValidationHelper.EsVacio(identidad))
                return ApiResponse<object>.Error(new List<string> { "El ID es obligatorio." });

            var affected = await db.Entidades
                .Where(model => model.IdEntidad == identidad)
                .ExecuteDeleteAsync();

            if (affected == 0)
            {
                await bitacora.RegistrarAsync(
                    usuario,
                    "Eliminar entidad",
                    "No encontrado",
                    $"No existe entidad con ID: {identidad}"
                );

                return ApiResponse<object>.NotFound("Entidad no encontrada");
            }

            await bitacora.RegistrarAsync(
                usuario,
                "Eliminar entidad",
                "Éxito",
                $"Se eliminó la entidad con ID: {identidad}"
            );

            return ApiResponse<object>.Success(null, "Entidad eliminada correctamente");
        })
        .WithName("DeleteEntidades")
        .WithOpenApi();

    }
}
