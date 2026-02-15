using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Services.Interfaces;
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

        var group = routes.MapGroup("/screen").RequireAuthorization();

        // Probar todas estas APIS en Postman o en el navegador
        // Con el punto Todos los datos son requeridos y no pueden ser vacíos ni espacios en blanco.

        /*==== METODOS GET ======*/

        /*------- METODOS GET TRAER TODOS LOS USUARIOS -------*/

        group.MapGet("/", async (
            IBitacoraService bitacora,
            [FromServices] PagosMovilesContext db,
            HttpContext context) =>
        {

            var usuario = context.User.Identity?.Name ?? "Usuario desconocido";

            var listaPantallas = await db.TablaPantallas.ToListAsync();

            await bitacora.RegistrarAccionBitacora(
                usuario: usuario,
                accion: "Obtener todas las pantallas",
                resultado: "Éxito",
                descripcion: $"Se obtuvieron {listaPantallas.Count} pantallas."
            );


            return await db.TablaPantallas.ToListAsync();
        })
        .WithName("GetAllTablaPantallas")
        .WithOpenApi();

        /*------- METODOS GET A LOS USUARIOS POR LLAVE PRIMARIA-------*/

        group.MapGet("/{id}", async Task<Results<Ok<TablaPantallas>, NotFound>> (
            int idpantalla, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitacora) =>
        {

            var usuario = "Usuario desconocido"; // Aquí podrías obtener el usuario autenticado desde el contexto
            var pantalla = await db.TablaPantallas
               .AsNoTracking()
               .FirstOrDefaultAsync(model => model.IdPantalla == idpantalla);

            if (pantalla is null)
            {
                await bitacora.RegistrarAccionBitacora(
                    usuario,
                    "Obtener pantalla por ID",
                    "No encontrado",
                    $"No se encontró la pantalla con ID {idpantalla}."
                );

                return TypedResults.NotFound();
            }

            await bitacora.RegistrarAccionBitacora(
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

        group.MapPut("/{idpantalla:int}", async Task<IResult> (int idpantalla,[FromBody] TablaPantallas tablaPantallas,[FromServices] PagosMovilesContext db) =>
        {

            if (idpantalla <= 0)
            {
                return Results.BadRequest(new
                {
                    succes = false,
                    status = 400,
                    error = new
                    {
                        code = "VALIDATION_ERROR",
                        message = "El IdPantalla debe ser un número positivo mayor que cero.",
                        details = new List<string> { "IdPantalla inválido." }
                    }
                });
            }

            var errores = new List<string>();

            if (string.IsNullOrEmpty(tablaPantallas.Nombre))
                errores.Add("El campo Nombre es obligatorio.");

            if (string.IsNullOrEmpty(tablaPantallas.Descripcion))
                errores.Add("El campo Descripcion es obligatorio.");

            if (string.IsNullOrEmpty(tablaPantallas.Ruta))
                errores.Add("El campo Ruta es obligatorio.");

            if (errores.Any())
            {
                return Results.BadRequest(new
                {
                    succes = false,
                    status = 400,
                    error = new
                    {
                        code = "VALIDATION_ERROR",
                        message = "Datos inválidos",
                        details = errores
                    }
                });
            }

            /* ===== BUSCAR REGISTRO ===== */
            var pantallaDb = await db.TablaPantallas.FindAsync(idpantalla);

            if (pantallaDb is null)
            {
                return Results.NotFound(new
                {
                    success = false,
                    status = 404,
                    error = new
                    {
                        code = "NOT_FOUND",
                        message = "La pantalla no existe"
                    }
                });
            }

            /* ===== ACTUALIZAR DATOS ===== */
            pantallaDb.Nombre = tablaPantallas.Nombre.Trim();
            pantallaDb.Descripcion = tablaPantallas.Descripcion.Trim();
            pantallaDb.Ruta = tablaPantallas.Ruta.Trim();

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                success = true,
                status = 200,
                message = "Pantalla actualizada correctamente",
                data = pantallaDb
            });

        })
        .WithName("UpdateTablaPantallas")
        .WithOpenApi();

        /*==== METODOS POST ======*/

        group.MapPost("/", async ([FromBody] TablaPantallas tablaPantallas,[FromServices] PagosMovilesContext db) =>
        {

            /*===== VALIDACIONES =====*/

            var errores = new List<string>();

            if (tablaPantallas.IdPantalla <=0)
                errores.Add("El campo IdPantalla debe ser un número positivo mayor que cero.");

            if (string.IsNullOrEmpty(tablaPantallas.Nombre))
                errores.Add("El campo Nombre es obligatorio.");

            if (string.IsNullOrEmpty(tablaPantallas.Descripcion))
                errores.Add("El campo Descripcion es obligatorio.");

            if (string.IsNullOrEmpty(tablaPantallas.Ruta))
                errores.Add("El campo Ruta es obligatorio.");

            // Verificar si el IdPantalla ya existe

            // Podria utilizar 409 Conflict pero en este caso usare 400 Bad Request para mantener la consistencia con otros errores de validacion

            var existente = await db.TablaPantallas
                .AnyAsync(id => id.IdPantalla == tablaPantallas.IdPantalla);

            if (existente)
                errores.Add($"El IdPantalla {tablaPantallas.IdPantalla} ya existe en el sistema.");
            {
                
            }

            if (errores.Any())
            {
                return Results.BadRequest(new
                {
                    succes = false,
                    status = 400,
                    error = new
                    {
                        code = "VALIDATION_ERROR",
                        message = "Datos inválidos",
                        details = errores
                    }
                });
            }


            db.TablaPantallas.Add(tablaPantallas);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/TablaPantallas/{tablaPantallas.IdPantalla}",tablaPantallas);
        })
        .WithName("CreateTablaPantallas")
        .WithOpenApi();

        /*==== METODOS DELETE ======*/

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (
            int idpantalla, 
            [FromServices] PagosMovilesContext db,
            [FromServices] IBitacoraService bitadora) =>
        {

            var affected = await db.TablaPantallas
            .Where(model => model.IdPantalla == idpantalla)
            .ExecuteDeleteAsync();

            if (affected == 0)
            {
                await bitadora.RegistrarAccionBitacora(
                    "Usuario desconocido",
                    "Eliminar pantalla",
                    "No encontrado",
                    $"No se encontró la pantalla con ID {idpantalla} para eliminar."
                );
                return TypedResults.NotFound();
            }

            await bitadora.RegistrarAccionBitacora(
                "Usuario desconocido",
                "Eliminar pantalla",
                "Éxito",
                $"Se eliminó la pantalla con ID {idpantalla}."
            );

            return TypedResults.NotFound();

        })
        .WithName("DeleteTablaPantallas")
        .WithOpenApi();
    }
}
