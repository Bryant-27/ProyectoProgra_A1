using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Repositories;
using Entities.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Logica_Negocio.Interfaces;
using Microsoft.Extensions.Logging;

namespace Logica_Negocio.Services
{
    public class MovimientosService : IMovimientosService
    {
        private readonly IAfiliacionRepository _afiliacionRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBitacoraService _bitacoraService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MovimientosService> _logger;

        public MovimientosService(
            IAfiliacionRepository afiliacionRepository,
            IHttpClientFactory httpClientFactory,
            IBitacoraService bitacoraService,
            IConfiguration configuration,
            ILogger<MovimientosService> logger)  // ← Agregado logger
        {
            _afiliacionRepository = afiliacionRepository;
            _httpClientFactory = httpClientFactory;
            _bitacoraService = bitacoraService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<MovimientosResponse> ObtenerUltimosMovimientosAsync(
            string telefono, string identificacion, string usuario)
        {
            try
            {
                _logger.LogInformation("SRV11 - Buscando afiliación para teléfono: {Telefono}", telefono);

                // 1. Buscar afiliación
                var afiliacion = await _afiliacionRepository
                    .GetByTelefonoAndIdentificacionAsync(telefono, identificacion);

                if (afiliacion == null)
                {
                    _logger.LogWarning("SRV11 - Cliente no asociado: Tel {Telefono}, ID {Identificacion}",
                        telefono, identificacion);

                    await _bitacoraService.RegistrarAsync(
                        usuario: usuario,
                        accion: "CONSULTA_MOVIMIENTOS",
                        resultado: "NO_AFILIADO",
                        descripcion: $"Cliente no asociado a pagos móviles. Tel: {telefono}",
                        servicio: "MovimientosService.ObtenerUltimosMovimientosAsync"
                    );

                    return new MovimientosResponse
                    {
                        Codigo = -1,
                        Descripcion = "Cliente no asociado a pagos móviles",
                        Movimientos = new List<MovimientoDto>()
                    };
                }

                _logger.LogInformation("SRV11 - Afiliación encontrada. Cuenta: {Cuenta}", afiliacion.NumeroCuenta);

                // 2. Consultar core bancario
                var coreResponse = await ConsultarCoreBancarioAsync(
                    afiliacion.IdentificacionUsuario,
                    afiliacion.NumeroCuenta);

                if (coreResponse == null || coreResponse.Codigo != 0)
                {
                    _logger.LogError("SRV11 - Error en core bancario: {Error}", coreResponse?.Descripcion);

                    await _bitacoraService.RegistrarAsync(
                        usuario: usuario,
                        accion: "CONSULTA_MOVIMIENTOS",
                        resultado: "ERROR_CORE",
                        descripcion: coreResponse?.Descripcion ?? "Error al consultar movimientos",
                        servicio: "MovimientosService.ObtenerUltimosMovimientosAsync"
                    );

                    return new MovimientosResponse
                    {
                        Codigo = -1,
                        Descripcion = coreResponse?.Descripcion ?? "Error al consultar movimientos",
                        Movimientos = new List<MovimientoDto>()
                    };
                }

                // 3. Éxito
                _logger.LogInformation("SRV11 - Consulta exitosa. {Count} movimientos encontrados",
                    coreResponse.Movimientos?.Count ?? 0);

                await _bitacoraService.RegistrarAsync(
                    usuario: usuario,
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "EXITO",
                    descripcion: $"Consulta exitosa. Cuenta: {afiliacion.NumeroCuenta}",
                    servicio: "MovimientosService.ObtenerUltimosMovimientosAsync"
                );

                return coreResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRV11 - Error inesperado: {Message}", ex.Message);

                await _bitacoraService.RegistrarAsync(
                    usuario: usuario,
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "ERROR",
                    descripcion: ex.Message,
                    servicio: "MovimientosService.ObtenerUltimosMovimientosAsync"
                );

                return new MovimientosResponse
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor",
                    Movimientos = new List<MovimientoDto>()
                };
            }
        }

        private async Task<MovimientosResponse?> ConsultarCoreBancarioAsync(
            string identificacion, string numeroCuenta)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var coreUrl = _configuration["CoreBancario:BaseUrl"]
                    ?? "https://localhost:5001";  // ← Ajusta según tu configuración

                var url = $"{coreUrl}/api/CoreBancario/transactions?identificacion={identificacion}&cuenta={numeroCuenta}";

                _logger.LogInformation("Consultando core bancario: {Url}", url);

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Core bancario respondió con status: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MovimientosResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando core bancario");
                return null;
            }
        }
    }
}