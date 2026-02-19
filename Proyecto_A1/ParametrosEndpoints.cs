using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Services.Interfaces;
namespace Proyecto_A1;

public static class ParametrosEndpoints
{
    public static void MapParametrosEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/parametro").RequireAuthorization().WithTags(nameof(Parametros));

        // ===== METODOS GET =====

        group.MapGet("/", async (
            [FromServices] IBitacoraService bitacora,
            [FromServices] PagosMovilesContext db,
            HttpContext context) =>
        {

            var usuario = context.User.Identity?.Name ?? "Usuario no autenticado";

            var lista = await db.Parametros.ToListAsync();

            await bitacora.RegistrarAccionBitacora(
                usuario,
                accion: "Obtener todas las pantallas",
                resultado: "Éxito",
                descripcion: $"Se obtuvieron {lista.Count} pantallas."
            );

            return await db.Parametros.ToListAsync();
        })
        .WithName("GetAllParametros")
        .WithOpenApi();

        // ===== METODOS GET POR ID =====

        group.MapGet("/{id}", async Task<Results<Ok<Parametros>, NotFound>> (
            string idparametro, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora) =>
        {
            var usuario = "Usuario no autenticado"; 
            var model = await db.Parametros
            .AsNoTracking()
            .FirstOrDefaultAsync(model => model.IdParametro == idparametro);

            if (model is null)
            {
                await bitacora.RegistrarAccionBitacora(
                    usuario,
                    accion: "Obtener pantalla por ID",
                    resultado: "No encontrado",
                    descripcion: $"No se encontró la pantalla con ID: {idparametro}."
                );
                return TypedResults.NotFound();
            }

            await bitacora.RegistrarAccionBitacora(
                    usuario,
                    accion: "Obtener pantalla por ID",
                    resultado: "Éxito",
                    descripcion: $"Se obtuvo la pantalla con ID: {idparametro}."
                );
            return TypedResults.Ok(model);

        })
        .WithName("GetParametrosById")
        .WithOpenApi();


        // ===== METODOS PUT =====

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (
            string idparametro,
            [FromBody] Parametros parametros, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            HttpContext context) =>
        {
            var affected = await db.Parametros
                .Where(model => model.IdParametro == idparametro)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.IdParametro, parametros.IdParametro)
                    .SetProperty(m => m.Valor, parametros.Valor)
                    .SetProperty(m => m.IdEstado, parametros.IdEstado)
                    .SetProperty(m => m.FechaCreacion, parametros.FechaCreacion)
                    );

            if (affected == 1)
            {
                await bitacora.RegistrarAccionBitacora(
                    context.User.Identity?.Name ?? "Usuario no autenticado",
                    accion: "Actualizar pantalla",
                    resultado: "Éxito",
                    descripcion: $"Se actualizó la pantalla con ID: {idparametro}."
                );
                return TypedResults.Ok();
            }

            await bitacora.RegistrarAccionBitacora(
                context.User.Identity?.Name ?? "Usuario no autenticado",
                accion: "Actualizar pantalla",
                resultado: "No encontrado",
                descripcion: $"No se encontró la pantalla con ID: {idparametro} para actualizar."
            );

            await db.SaveChangesAsync();

            return TypedResults.Ok();
            
        })
        .WithName("UpdateParametros")
        .WithOpenApi();

        // ===== METODOS POST =====

        group.MapPost("/", async (
            [FromBody] Parametros parametros, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario Desconocido";

            db.Parametros.Add(parametros);
            await db.SaveChangesAsync();

            await bitacora.RegistrarAccionBitacora(
                usuario,
                accion: "Crear pantalla",
                resultado: "Éxito",
                descripcion: $"Se creó la pantalla con ID: {parametros.IdParametro}."
            );

            return TypedResults.Created($"/api/Parametros/{parametros.IdParametro}",parametros);
        })
        .WithName("CreateParametros")
        .WithOpenApi();

        // ===== METODOS DELETE =====

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (
            string idparametro, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora) =>
        {
            var affected = await db.Parametros
                .Where(model => model.IdParametro == idparametro)
                .ExecuteDeleteAsync();

            if (affected == 0) 
            { 
            
                await bitacora.RegistrarAccionBitacora(
                    "Usuario no autenticado",
                    accion: "Eliminar pantalla",
                    resultado: "No encontrado",
                    descripcion: $"No se encontró la pantalla con ID: {idparametro} para eliminar."
                );

                return TypedResults.NotFound();

            }

            await bitacora.RegistrarAccionBitacora(
                "Usuario no autenticado",
                accion: "Eliminar pantalla",
                resultado: "Éxito",
                descripcion: $"Se eliminó la pantalla con ID: {idparametro}."
            );

            return TypedResults.Ok();
        })
        .WithName("DeleteParametros")
        .WithOpenApi();
    }
}
