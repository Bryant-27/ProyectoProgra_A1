using Entities.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Logica_Negocio.Services
{
    public class TransaccionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransaccionService> _logger;

        public TransaccionService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TransaccionService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        // SRV7 - Procesar transacción recibida
        public async Task<TransaccionResponse> ProcessTransactionAsync(TransaccionRequest request)
        {
            try
            {
                _logger.LogInformation("SRV7 - Procesando transacción");

                // Validaciones
                var validationResult = await ValidateTransactionRequest(request);
                if (validationResult != null)
                    return validationResult;

                // Aquí llamarías a SRV12 para enrutar
                // Por ahora simulamos éxito
                await Task.Delay(100);

                return new TransaccionResponse
                {
                    Codigo = 0,
                    Descripcion = "Transacción aplicada"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SRV7");
                return new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                };
            }
        }

        // SRV8 - Enviar transacción a entidad externa
        public async Task<TransaccionResponse> SendTransactionAsync(EnvioTransaccionRequest request, string token)
        {
            try
            {
                _logger.LogInformation("SRV8 - Enviando transacción a externo");

                // Validar token (simulado)
                if (string.IsNullOrEmpty(token) || token.Length < 10)
                {
                    return new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = "Token inválido"
                    };
                }

                // Validaciones
                var validationResult = await ValidateSendRequest(request);
                if (validationResult != null)
                    return validationResult;

                // Simular envío a entidad externa
                await Task.Delay(500);

                // Simular respuesta (80% éxito)
                var random = new Random().Next(1, 101);

                if (random <= 80)
                {
                    return new TransaccionResponse
                    {
                        Codigo = 0,
                        Descripcion = "Transacción enviada exitosamente a entidad externa"
                    };
                }
                else
                {
                    return new TransaccionResponse
                    {
                        Codigo = -1,
                        Descripcion = "Error en entidad externa: saldo insuficiente"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SRV8");
                return new TransaccionResponse
                {
                    Codigo = -1,
                    Descripcion = "Error interno del servidor"
                };
            }
        }

        // Validaciones para SRV7
        private async Task<TransaccionResponse> ValidateTransactionRequest(TransaccionRequest request)
        {
            if (request == null)
                return ErrorResponse("Debe enviar los datos completos y válidos");

            if (string.IsNullOrWhiteSpace(request.TelefonoOrigen) ||
                string.IsNullOrWhiteSpace(request.NombreOrigen) ||
                string.IsNullOrWhiteSpace(request.TelefonoDestino) ||
                string.IsNullOrWhiteSpace(request.Descripcion))
                return ErrorResponse("Debe enviar los datos completos y válidos");

            if (request.TelefonoOrigen.Length != 8 || !request.TelefonoOrigen.All(char.IsDigit))
                return ErrorResponse("El teléfono origen debe tener 8 dígitos numéricos");

            if (request.TelefonoDestino.Length != 8 || !request.TelefonoDestino.All(char.IsDigit))
                return ErrorResponse("El teléfono destino debe tener 8 dígitos numéricos");

            if (request.Descripcion.Length > 25)
                return ErrorResponse("La descripción no puede superar 25 caracteres");

            if (request.Monto <= 0 || request.Monto > 100000)
                return ErrorResponse("El monto no debe ser superior a 100.000");

            var grupoEntidadId = _configuration.GetValue<int>("AppSettings:GrupoEntidadId");
            if (request.EntidadDestino != grupoEntidadId)
                return ErrorResponse("La entidad destino no es válida");

            if (request.EntidadOrigen != 1) // Simulado
                return ErrorResponse("La entidad origen no está registrada o no está activa");

            return null;
        }

        // Validaciones para SRV8
        private async Task<TransaccionResponse> ValidateSendRequest(EnvioTransaccionRequest request)
        {
            if (request == null)
                return ErrorResponse("Debe enviar los datos completos y válidos");

            if (string.IsNullOrWhiteSpace(request.TelefonoOrigen) ||
                string.IsNullOrWhiteSpace(request.NombreOrigen) ||
                string.IsNullOrWhiteSpace(request.TelefonoDestino) ||
                string.IsNullOrWhiteSpace(request.Descripcion))
                return ErrorResponse("Debe enviar los datos completos y válidos");

            var grupoEntidadId = _configuration.GetValue<int>("AppSettings:GrupoEntidadId");
            if (request.EntidadOrigen != grupoEntidadId)
                return ErrorResponse("La entidad origen debe ser la entidad del grupo");

            if (request.TelefonoOrigen.Length != 8 || !request.TelefonoOrigen.All(char.IsDigit))
                return ErrorResponse("El teléfono origen debe tener 8 dígitos numéricos");

            if (request.TelefonoDestino.Length != 8 || !request.TelefonoDestino.All(char.IsDigit))
                return ErrorResponse("El teléfono destino debe tener 8 dígitos numéricos");

            if (request.Descripcion.Length > 25)
                return ErrorResponse("La descripción no puede superar 25 caracteres");

            if (request.Monto <= 0 || request.Monto > 100000)
                return ErrorResponse("El monto no debe ser superior a 100.000");

            // Validar que teléfono origen existe (simulado)
            var telefonosValidos = new[] { "88881111", "88882222", "88883333", "88884444" };
            if (!telefonosValidos.Contains(request.TelefonoOrigen))
                return ErrorResponse("El teléfono origen no está registrado en pagos móviles");

            return null;
        }

        private TransaccionResponse ErrorResponse(string mensaje)
        {
            return new TransaccionResponse
            {
                Codigo = -1,
                Descripcion = mensaje
            };
        }
    }
}