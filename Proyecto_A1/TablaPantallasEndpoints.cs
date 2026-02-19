using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Services.Interfaces;
using Proyecto_A1.Helper;
namespace Proyecto_A1;

public static class TablaPantallasEndpoints
{
    public static void MapTablaPantallasEndpoints (this IEndpointRouteBuilder routes)
    {

        // Se cambia a RequireAuthorization para proteger las rutas y
        // requerir autenticación para acceder a ellas. Esto asegura
        // que solo los usuarios autenticados puedan interactuar con
        // los endpoints relacionados con TablaPantallas, mejorando
        // la seguridad de la aplicación.

        var group = routes.MapGroup("/screen").RequireAuthorization().WithTags(nameof(TablaPantallas));


        // Probar todas estas APIS en Postman o en el navegador
        // Con el punto Todos los datos son requeridos y no pueden ser vacíos ni espacios en blanco.

        /*==== METODOS GET ======*/

        /*------- METODOS GET TRAER TODOS LOS USUARIOS -------*/

        group.MapGet("/", async (
            [FromServices] IBitacoraService bitacora,
            [FromServices] PagosMovilesContext db,
            HttpContext context) =>
        {

            var usuario = context.User.Identity?.Name ?? "Usuario desconocido";

            var listaPantallas = await db.TablaPantallas.ToListAsync();

            await bitacora.RegistrarAsync(
                usuario: usuario,
                accion: "Obtener todas las pantallas",
                resultado: "Éxito",
                descripcion: $"Se obtuvieron {listaPantallas.Count} pantallas."
            );


            return await db.TablaPantallas.ToListAsync();
        })
        .WithName("GetAllTablaPantallas")
        .WithOpenApi();

        /*========= METODOS GET A LOS USUARIOS POR LLAVE PRIMARIA ==========*/

        group.MapGet("/{id}", async Task<Results<Ok<TablaPantallas>, NotFound>> (
            int idpantalla, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora) =>
        {

            var usuario = "Usuario desconocido"; 
            var pantalla = await db.TablaPantallas
               .AsNoTracking()
               .FirstOrDefaultAsync(model => model.IdPantalla == idpantalla);

            if (pantalla is null)
            {
                await bitacora.RegistrarAsync(
                    usuario,
                    "Obtener pantalla por ID",
                    "No encontrado",
                    $"No se encontró la pantalla con ID {idpantalla}."
                );

                return TypedResults.NotFound();
            }

            await bitacora.RegistrarAsync(
                usuario,
                "Obtener pantalla por ID",
                "Éxito",
                $"Se obtuvo la pantalla con ID {idpantalla}."
                );

            return TypedResults.Ok(pantalla);

        })
        .WithName("GetTablaPantallasById")
        .WithOpenApi();

        /*==== METODOS PUT ======*/

        group.MapPut("/{id}", async (
            int idpantalla,
            [FromBody] TablaPantallas pantalla,
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora,
            HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario desconocido";

            var errores = ValidationHelper.ValidarModelo(pantalla);

            if (idpantalla <= 0)
                errores.Add("El IdPantalla es inválido.");

            if (errores.Any())
                return ApiResponse<TablaPantallas>.Error(errores);

            var existe = await db.TablaPantallas.AnyAsync(x => x.IdPantalla == idpantalla);

            if (!existe)
            {
                await bitacora.RegistrarAsync(
                    "Sistema",
                    "Actualizar Usuario",
                    "Exitoso",
                    $"Usuario {idpantalla} actualizado",
                    "UsuariosEndpoint - PUT"
                );

                return ApiResponse<TablaPantallas>.NotFound("Pantalla no encontrada");
            }

            await bitacora.RegistrarAsync(
                "Sistema",
                "Actualizar Pantalla",
                "No encontrado",
                $"Intento de actualizar pantalla {idpantalla}",
                "UsuariosEndpoint - PUT"
            );

            return ApiResponse<TablaPantallas>.Success(null, "Pantalla actualizada");

        })
        .WithName("UpdateTablaPantallas")
        .WithOpenApi();

        /*==== METODOS POST ======*/

        group.MapPost("/", async (
         [FromBody] TablaPantallas pantalla,
         [FromServices] PagosMovilesContext db,
         [FromServices] IBitacoraService bitacora,
         HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario desconocido";

            var errores = ValidationHelper.ValidarModelo(pantalla);

            if (pantalla.IdPantalla <= 0)
                errores.Add("El IdPantalla debe ser mayor que cero.");

            if (errores.Any())
                return ApiResponse<TablaPantallas>.Error(errores);

            var existe = await db.TablaPantallas.AnyAsync(x => x.IdPantalla == pantalla.IdPantalla);

            if (existe)
                return ApiResponse<TablaPantallas>.Error(
                    new List<string> { "El IdPantalla ya existe." });

            db.TablaPantallas.Add(pantalla);
            await db.SaveChangesAsync();

            await bitacora.RegistrarAsync(
               "Sistema",
               "Crear pantalla",
               "Éxito",
               $"Se creó la pantalla con ID {tablaPantallas.IdPantalla}."
           );

            return ApiResponse<TablaPantallas>.Success(pantalla, "Pantalla creada correctamente");
        })
        .WithName("CreateTablaPantallas")
        .WithOpenApi();

        /*==== METODOS DELETE ======*/

        group.MapDelete("/{id}", async (
         int idpantalla,
         [FromServices] PagosMovilesContext db,
         [FromServices] IBitacoraService bitacora,
         HttpContext context) =>
        {
            var usuario = context.User.Identity?.Name ?? "Usuario desconocido";

            var affected = await db.TablaPantallas
                .Where(x => x.IdPantalla == idpantalla)
                .ExecuteDeleteAsync();

            if (affected == 0)
            {
                await bitadora.RegistrarAsync(
                    "Usuario desconocido",
                    "Eliminar pantalla",
                    "No encontrado",
                    $"Pantalla {idpantalla} no existe"
                );

                return ApiResponse<object>.NotFound("Pantalla no encontrada");
            }

            await bitadora.RegistrarAsync(
                "Usuario desconocido",
                "Eliminar pantalla",
                "Éxito",
                $"Pantalla {idpantalla} eliminada"
            );

            return ApiResponse<object>.Success(null, "Pantalla eliminada");

        })
        .WithName("DeleteTablaPantallas")
        .WithOpenApi();
    }
}
