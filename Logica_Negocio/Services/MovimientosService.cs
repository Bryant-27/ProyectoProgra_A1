using DataAccess.Repositories;
using Entities.DTOs;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;

namespace Logica_Negocio.Services
{


    public class MovimientosService : IMovimientosService
    {
        private readonly IAfiliacionRepository _afiliacionRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MovimientosService(
            IAfiliacionRepository afiliacionRepository,
            IBitacoraService bitacoraService,
            HttpClient httpClient,
            IConfiguration configuration)  // ← Inyectar IConfiguration
        {
            _afiliacionRepository = afiliacionRepository;
            _bitacoraService = bitacoraService;
            _httpClient = httpClient;
            _configuration = configuration;

            // Configurar BaseAddress aquí con validación
            var coreUrl = _configuration["Services:CoreBancarioUrl"];
            if (string.IsNullOrWhiteSpace(coreUrl))
            {
                throw new InvalidOperationException("Falta configuración: Services:CoreBancarioUrl");
            }

            _httpClient.BaseAddress = new Uri(coreUrl);
        }

        public async Task<MovimientosResponse> ObtenerUltimosMovimientosAsync(
            string telefono, string identificacion, string usuario)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(telefono) || string.IsNullOrWhiteSpace(identificacion))
            {
                return new MovimientosResponse
                {
                    Codigo = -1,
                    Descripcion = "Debe enviar los datos completos y válidos",
                    Movimientos = new List<MovimientoDto>()
                };
            }

            try
            {
                // Buscar afiliación
                var afiliacion = await _afiliacionRepository
                    .GetByTelefonoAndIdentificacionAsync(telefono, identificacion);

                if (afiliacion == null)
                {
                    await _bitacoraService.RegistrarAsync(
                        usuario: usuario,
                        accion: "CONSULTA_MOVIMIENTOS",
                        resultado: "ERROR",
                        descripcion: $"Cliente no afiliado: {telefono}",
                        servicio: "SRV11");

                    return new MovimientosResponse
                    {
                        Codigo = -1,
                        Descripcion = "Cliente no asociado a pagos móviles",
                        Movimientos = new List<MovimientoDto>()
                    };
                }

                // Llamar al API de Core Bancario (SRV16)
                var url = $"api/core/transactions?identificacion={afiliacion.IdentificacionUsuario}&cuenta={afiliacion.NumeroCuenta}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await _bitacoraService.RegistrarAsync(
                        usuario: usuario,
                        accion: "CONSULTA_MOVIMIENTOS",
                        resultado: "ERROR",
                        descripcion: $"Error Core Bancario: {errorContent}",
                        servicio: "SRV11");

                    return new MovimientosResponse
                    {
                        Codigo = -1,
                        Descripcion = "Error al consultar movimientos en Core Bancario",
                        Movimientos = new List<MovimientoDto>()
                    };
                }

                // Deserializar respuesta
                var content = await response.Content.ReadAsStringAsync();
                var coreResponse = JsonSerializer.Deserialize<CoreMovimientosResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (coreResponse?.Movimientos == null || !coreResponse.Movimientos.Any())
                {
                    return new MovimientosResponse
                    {
                        Codigo = 0,
                        Descripcion = "No se encontraron movimientos",
                        Movimientos = new List<MovimientoDto>()
                    };
                }

                // Mapear a DTO
                var movimientos = coreResponse.Movimientos.Select(m => new MovimientoDto
                {
                    MovimientoId = m.MovimientoId,
                    FechaMovimiento = m.FechaMovimiento,
                    Monto = m.Monto,
                    TipoMovimiento = m.TipoMovimiento,
                    Descripcion = m.Descripcion ?? string.Empty,
                    SaldoAnterior = m.SaldoAnterior,
                    SaldoNuevo = m.SaldoNuevo
                }).ToList();

                await _bitacoraService.RegistrarAsync(
                    usuario: usuario,
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "EXITO",
                    descripcion: $"Cuenta: {afiliacion.NumeroCuenta}, Movs: {movimientos.Count}",
                    servicio: "SRV11");

                return new MovimientosResponse
                {
                    Codigo = 0,
                    Descripcion = "Consulta exitosa",
                    Movimientos = movimientos
                };
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(
                    usuario: usuario,
                    accion: "CONSULTA_MOVIMIENTOS",
                    resultado: "ERROR",
                    descripcion: $"Excepción: {ex.Message}",
                    servicio: "SRV11");

                return new MovimientosResponse
                {
                    Codigo = -1,
                    Descripcion = "Error al consultar movimientos",
                    Movimientos = new List<MovimientoDto>()
                };
            }
        }
    }

    // DTOs para deserializar respuesta del Core
    public class CoreMovimientosResponse
    {
        public int Codigo { get; set; }
        public string Descripcion { get; set; }
        public List<CoreMovimientoDto> Movimientos { get; set; }
    }

    public class CoreMovimientoDto
    {
        public long MovimientoId { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public decimal Monto { get; set; }
        public string TipoMovimiento { get; set; }
        public string Descripcion { get; set; }
        public decimal SaldoAnterior { get; set; }
        public decimal SaldoNuevo { get; set; }
    }
}