using Microsoft.AspNetCore.Mvc;
using DataAccess.Models;
using Servicios;
using Microsoft.AspNetCore.Identity.Data;
namespace Proyecto_A1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutenticacionController : ControllerBase
    {
        private readonly ServicioAutenticacion _servicio;

        public AutenticacionController(ServicioAutenticacion servicio)
        {
            _servicio = servicio;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var sesion = await _servicio.ValidarYAutenticar(request.IdUsuario, request.Nombre, request.Password);

            if (sesion == null)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos" });
            }

            return Ok(sesion);
        }

        public class LoginRequest
        {
            public int IdUsuario { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
