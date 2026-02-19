using Microsoft.AspNetCore.Mvc;
using Logica_Negocio.Interfaces;
using Logica_Negocio.Models;
using Entities.DTOs;  // ← Cambiado: ahora usa Entities.DTOs

namespace Proyecto_A1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticacionController : ControllerBase
    {
        private readonly IAutenticacionService _servicioAutenticacion;

        public AutenticacionController(IAutenticacionService servicioAutenticacion)
        {
            _servicioAutenticacion = servicioAutenticacion;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Usuario) ||
                    string.IsNullOrWhiteSpace(request.Contrasena))
                {
                    return BadRequest(new
                    {
                        codigo = -1,
                        descripcion = "Usuario y contraseña son requeridos"
                    });
                }

                var sesion = await _servicioAutenticacion.ValidarYAutenticar(
                    request.IdUsuario.ToString(),
                    request.Usuario,
                    request.Contrasena);

                if (sesion == null)
                {
                    return Unauthorized(new
                    {
                        codigo = -1,
                        descripcion = "Usuario o contraseña incorrectos"
                    });
                }

                var response = new LoginResponse
                {
                    ExpiresIn = sesion.FechaExpiracion,
                    AccessToken = sesion.Token,
                    RefreshToken = sesion.RefreshToken ?? string.Empty,
                    UsuarioID = int.Parse(sesion.IdUsuario)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    codigo = -1,
                    descripcion = "Error interno del servidor"
                });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return BadRequest(new { codigo = -1, descripcion = "Refresh token requerido" });
                }

                var sesion = await _servicioAutenticacion.RefrescarToken(request.RefreshToken);

                if (sesion == null)
                {
                    return Unauthorized(new { codigo = -1, descripcion = "Token inválido o expirado" });
                }

                return Ok(new
                {
                    codigo = 0,
                    descripcion = "Token renovado",
                    access_token = sesion.Token,
                    refresh_token = sesion.RefreshToken,
                    expires_in = sesion.FechaExpiracion
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { codigo = -1, descripcion = "Error interno" });
            }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new { codigo = -1, descripcion = "Token requerido" });
                }

                var esValido = await _servicioAutenticacion.ValidarToken(token);

                if (esValido)
                {
                    return Ok(new { codigo = 0, descripcion = "Token válido" });
                }
                else
                {
                    return Unauthorized(new { codigo = -1, descripcion = "Token inválido" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { codigo = -1, descripcion = "Error interno" });
            }
        }
    }
}