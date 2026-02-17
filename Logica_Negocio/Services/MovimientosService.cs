using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Repositories;
using Entities.DTOs;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;  // ← Para IHttpClientFactory
namespace Logica_Negocio.Services
{
    public class MovimientosService : IMovimientosService
    {
        private readonly IAfiliacionRepository _afiliacionRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBitacoraService _bitacoraService;
        private readonly IConfiguration _configuration;

        public MovimientosService(
            IAfiliacionRepository afiliacionRepository,
            IHttpClientFactory httpClientFactory,
            IBitacoraService bitacoraService,
            IConfiguration configuration)
        {
            _afiliacionRepository = afiliacionRepository;
            _httpClientFactory = httpClientFactory;
            _bitacoraService = bitacoraService;
            _configuration = configuration;
        }

        public async Task<MovimientosResponse> ObtenerUltimosMovimientosAsync(
            string telefono, string identificacion, string usuario)
        {
            // 1. Buscar afiliación
            var afiliacion = await _afiliacionRepository
                .GetByTelefonoAndIdentificacionAsync(telefono, identificacion);

            if (afiliacion == null)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario,
                    "CONSULTA_MOVIMIENTOS",
                    $"Cliente no asociado a pagos móviles. Tel: {telefono}",
                    "SRV11",
                    "ERROR");

                return new MovimientosResponse
                {
                    Codigo = -1,
                    Descripcion = "Cliente no asociado a pagos móviles",
                    Movimientos = new List<MovimientoDto>()
                };
            }

            var coreResponse = await ConsultarCoreBancarioAsync(
                afiliacion.IdentificacionUsuario,   // ← Sin guión bajo
                afiliacion.NumeroCuenta);            // ← Sin guión bajo

            if (coreResponse == null || coreResponse.Codigo != 0)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario,
                    "CONSULTA_MOVIMIENTOS",
                    $"Error core bancario: {coreResponse?.Descripcion}",
                    "SRV11",
                    "ERROR");

                return new MovimientosResponse
                {
                    Codigo = -1,
                    Descripcion = coreResponse?.Descripcion ?? "Error al consultar movimientos",
                    Movimientos = new List<MovimientoDto>()
                };
            }

            // 3. Éxito
            await _bitacoraService.RegistrarAsync(
                usuario,
                "CONSULTA_MOVIMIENTOS",
                $"Consulta exitosa. Cuenta: {afiliacion.NumeroCuenta}", // ← Sin guión bajo
                "SRV11",
                "EXITOSO");

            return coreResponse;
        }

        private async Task<MovimientosResponse> ConsultarCoreBancarioAsync(
            string identificacion, string numeroCuenta)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var coreUrl = _configuration["CoreBancario:BaseUrl"]
                    ?? "https://localhost:7001";

                var response = await client.GetAsync(
                    $"{coreUrl}/core/transactions?identificacion={identificacion}&cuenta={numeroCuenta}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MovimientosResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }
    }
}