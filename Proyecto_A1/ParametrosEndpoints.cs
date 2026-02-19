using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Services.Interfaces;
using Proyecto_A1.Helper;
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
                accion: "Obtener todas los Parametro",
                resultado: "Éxito",
                descripcion: $"Se obtuvieron {lista.Count} parametros."
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

        group.MapPut("/{id}", async (
            string idparametro,
            [FromBody] Parametros parametros,
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario no autenticado";

            var errores = ValidationHelper.ValidarModelo(parametros);

            if (ValidationHelper.EsVacio(parametros.IdParametro))
                errores.Add("El IdParametro es obligatorio.");

            if (ValidationHelper.EsVacio(parametros.Valor))
                errores.Add("El valor es obligatorio.");

            if (errores.Any())
                return ApiResponse<Parametros>.Error(errores);

            var affected = await db.Parametros
                .Where(m => m.IdParametro == idparametro)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Valor, parametros.Valor.Trim())
                    .SetProperty(m => m.IdEstado, parametros.IdEstado)
                    .SetProperty(m => m.FechaCreacion, parametros.FechaCreacion)
                );

            if (affected == 0)
            {
                await bitacora.RegistrarAccionBitacora(
                    usuario,
                    "Actualizar parámetro",
                    "No encontrado",
                    $"No existe el parámetro {idparametro}"
                );

                return ApiResponse<Parametros>.NotFound("El parámetro no existe");
            }

            await bitacora.RegistrarAccionBitacora(
                usuario,
                "Actualizar parámetro",
                "Éxito",
                $"Parámetro {idparametro} actualizado"
            );

            return ApiResponse<Parametros>.Success(null, "Parámetro actualizado correctamente");

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

            var errores = ValidationHelper.ValidarModelo(parametros);

            if (ValidationHelper.EsVacio(parametros.IdParametro))
                errores.Add("El IdParametro es obligatorio.");

            if (ValidationHelper.EsVacio(parametros.Valor))
                errores.Add("El valor es obligatorio.");

            var existe = await db.Parametros
                .AnyAsync(p => p.IdParametro == parametros.IdParametro);

            if (existe)
                errores.Add($"El parámetro {parametros.IdParametro} ya existe.");

            if (errores.Any())
                return ApiResponse<Parametros>.Error(errores);

            db.Parametros.Add(parametros);
            await db.SaveChangesAsync();

            await bitacora.RegistrarAccionBitacora(
                usuario,
                "Crear parámetro",
                "Éxito",
                $"Parámetro {parametros.IdParametro} creado"
            );

            return ApiResponse<Parametros>.Success(parametros, "Parámetro creado correctamente");
        })
        .WithName("CreateParametros")
        .WithOpenApi();

        // ===== METODOS DELETE =====

        group.MapDelete("/{id}", async (
            string idparametro,
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario no autenticado";

            var affected = await db.Parametros
                .Where(p => p.IdParametro == idparametro)
                .ExecuteDeleteAsync();

            if (affected == 0)
            {
                await bitacora.RegistrarAccionBitacora(
                    usuario,
                    "Eliminar parámetro",
                    "No encontrado",
                    $"No existe el parámetro {idparametro}"
                );

                return ApiResponse<Parametros>.NotFound("El parámetro no existe");
            }

            await bitacora.RegistrarAccionBitacora(
                usuario,
                "Eliminar parámetro",
                "Éxito",
                $"Parámetro {idparametro} eliminado"
            );

            return ApiResponse<Parametros>.Success(null, "Parámetro eliminado correctamente");
        })
        .WithName("DeleteParametros")
        .WithOpenApi();
    }
}
