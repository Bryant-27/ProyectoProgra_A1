using Microsoft.EntityFrameworkCore;
using DataAccess.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Logica_Negocio.Services.Interfaces;

namespace Proyecto_A1;

public static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints(this IEndpointRouteBuilder routes)
    {

        var group = routes.MapGroup("/endpoint/user").RequireAuthorization();

        // [FromServices] se agrega para inyectar el contexto de la base de datos
        // [FromServices] se utiliza para indicar que el PagosMovilesContext
        // no proviene de la solicitud HTTP (body, ruta o query),
        // sino que debe ser inyectado desde el contenedor de dependencias (DI).
        // Esto evita que ASP.NET Core intente inferirlo como un parámetro del body,
        // lo cual provocaría un error en tiempo de ejecución, especialmente en
        // endpoints GET donde no existe cuerpo en la petición.


        /*==== METODOS GET ======*/

        /*------- METODOS GET TRAER TODOS LOS USUARIOS -------*/

        group.MapGet("/", async (
            IBitacoraService bitacora,
            [FromServices] PagosMovilesContext db,
            HttpContext context) =>
        {

            var usuario = context.User.Identity?.Name ?? "Usuario desconocido";

            var lista = await db.Usuarios.ToListAsync();

            await bitacora.RegistrarAccionBitacora(
                usuario,
                "GET: CONSULTA_GENERAL",
                "ÉXITO",
                "Consulta de todos los usuarios realizada con éxito",
                "API Usuarios"
            );

            return Results.Ok(lista);

        })
        .WithName("GetAllUsuarios")
        .WithOpenApi();

        /*------- METODOS GET TRAER A LOS USUARIOS POR ID -------*/

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

        /*------- METODOS GET TRAER A LOS USUARIOS POR IDENTIFICACION, NOMBRE Y TIPO -------*/

        group.MapGet("/filtro", async ([FromQuery] string? identificacion, [FromQuery] string? nombre, [FromQuery] string? tipo, [FromServices] PagosMovilesContext db) =>
        {
            var query = db.Usuarios.AsNoTracking().AsQueryable();

            var usuarios = await db.Usuarios.Include(u => u.TipoIdentificacionNavigation).ToListAsync();


            if (!string.IsNullOrEmpty(identificacion))
            {
                query = query.Where(u => u.Identificacion.Contains(identificacion));
            }
            if (!string.IsNullOrEmpty(nombre))
            {
                query = query.Where(u => u.NombreCompleto.Contains(nombre));
            }
            if (!string.IsNullOrEmpty(tipo))
            {
                // Convertimos el 'tipo' que viene por parámetro (string) a int cambio realizado
                //modelos usuario 
                if (int.TryParse(tipo, out int tipoId))
                {
                    query = query.Where(u => u.TipoIdentificacionNavigation.IdIdentificacion == tipoId);
                }
            }
            if (string.IsNullOrWhiteSpace(identificacion) && string.IsNullOrWhiteSpace(nombre) && string.IsNullOrWhiteSpace(tipo))
            {
                return Results.BadRequest(new
                {
                    error = "Debe especificar al menos un criterio de búsqueda"
                });
            }

            var results = await query.ToListAsync();
            return Results.Ok(results);
        })
        .WithName("GetUsersWithFilters")
        .WithOpenApi();

        /*======= METODO PUT ======*/

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int idusuario, Usuarios usuarios, [FromServices] PagosMovilesContext db) =>
        {
            var affected = await db.Usuarios
                .Where(model => model.IdUsuario == idusuario)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.IdUsuario, usuarios.IdUsuario)
                    .SetProperty(m => m.NombreCompleto, usuarios.NombreCompleto)
                    .SetProperty(m => m.TipoIdentificacionNavigation.TipoIdentificacion, usuarios.TipoIdentificacionNavigation.TipoIdentificacion)
                    .SetProperty(m => m.Identificacion, usuarios.Identificacion)
                    .SetProperty(m => m.Email, usuarios.Email)
                    .SetProperty(m => m.Telefono, usuarios.Telefono)
                    .SetProperty(m => m.Usuario, usuarios.Usuario)
                    .SetProperty(m => m.Contraseña, usuarios.Contraseña) // Considerar hashear la contraseña aquí si es necesario o crear un endpoint aparte mas adelante
                    .SetProperty(m => m.IdEstado, usuarios.IdEstado)
                    .SetProperty(m => m.IdRol, usuarios.IdRol)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateUsuarios")
        .WithOpenApi();

        /*======= METODO POST ======*/

        group.MapPost("/", async (Usuarios usuarios, [FromServices] PagosMovilesContext db) =>
        {

            /*===== VALIDACIONES =====*/

            if (string.IsNullOrWhiteSpace(usuarios.NombreCompleto))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    status = 400,
                    error = new
                    {
                        code = "VALIDATION_ERROR",
                        message = "Datos inválidos",
                        details = new[]
                        {
                            "El nombre completo no puede estar vacío ni contener solo espacios"
                        }
                    }
                });
            }

            if (string.IsNullOrWhiteSpace(usuarios.Email))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    status = 400,
                    error = new
                    {
                        code = "VALIDATION_ERROR",
                        message = "Datos inválidos",
                        details = new[]
                        {
                            "El formato del email no es válido"
                        }
                    }
                });

            }

            try
            {
                var mail = new System.Net.Mail.MailAddress(usuarios.Email);
            }
            catch
            {
                return Results.BadRequest(new
                {
                    error = "El formato del email no es válido."
                });
            }

            // Hashear de la contraseña 

            var passwordHasher = new PasswordHasher<Usuarios>();

            usuarios.Contraseña = passwordHasher.HashPassword(usuarios, usuarios.Contraseña);

            db.Usuarios.Add(usuarios);
            await db.SaveChangesAsync();

            return TypedResults.Created(
                $"/api/Usuarios/{usuarios.IdUsuario}", usuarios.IdUsuario);


        })
        .WithName("CreateUsuarios")
        .WithOpenApi();

        /*======= METODO DELETE ======*/


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
