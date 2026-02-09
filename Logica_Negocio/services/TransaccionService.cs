using System;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace Logica_Negocio.Services
{
    public class TransaccionService
    {
        private readonly PagosMovilesContext _context;
        private readonly AuthService _authService;

        public TransaccionService(PagosMovilesContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        // Método auxiliar para validar transacciones
        public bool ValidarTransaccion(TransaccionRequest request)
        {
            if (request == null)
                return false;

            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(request.EntidadOrigen) ||
                string.IsNullOrWhiteSpace(request.EntidadDestino) ||
                string.IsNullOrWhiteSpace(request.TelefonoOrigen) ||
                string.IsNullOrWhiteSpace(request.NombreOrigen) ||
                string.IsNullOrWhiteSpace(request.TelefonoDestino) ||
                request.Monto <= 0)
                return false;

            // Validar longitud de descripción (max 25 caracteres según HU)
            if (!string.IsNullOrWhiteSpace(request.Descripcion) &&
                request.Descripcion.Length > 25)
                return false;

            // Validar monto máximo
            if (request.Monto > 100000.00m)
                return false;

            // Validar formato de teléfono
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.TelefonoOrigen, @"^\d{8,15}$") ||
                !System.Text.RegularExpressions.Regex.IsMatch(request.TelefonoDestino, @"^\d{8,15}$"))
                return false;

            return true;
        }

        // HU SRV7: Recibir transacciones (externas) - NO requiere token
        public async Task<TransaccionResponse> RecibirTransaccionAsync(TransaccionRequest request)
        {
            try
            {
                // 1. Validar datos básicos
                if (!ValidarTransaccion(request))
                    return new TransaccionResponse(-1, "Debe enviar los datos completos y válidos");

                // 2. Validar que la entidad origen exista
                var entidadOrigen = await _context.Entidades
                    .Include(e => e.IdEstadoNavigation)
                    .FirstOrDefaultAsync(e => e.NombreInstitucion == request.EntidadOrigen && e.IdEstado == 1);

                if (entidadOrigen == null)
                    return new TransaccionResponse(-1, "Entidad origen no registrada");

                // 3. Validar que la entidad destino sea la del grupo (GRUPO01)
                var entidadDestino = await _context.Entidades
                    .FirstOrDefaultAsync(e => e.NombreInstitucion == request.EntidadDestino && e.IdEstado == 1);

                if (entidadDestino == null)
                    return new TransaccionResponse(-1, "Entidad destino no válida");

                // 4. Validar monto máximo
                if (request.Monto > 100000.00m)
                    return new TransaccionResponse(-1, "El monto no debe ser superior a 100.000");

                // 5. Validar límite diario por teléfono
                var fechaHoy = DateTime.Today;
                var fechaManana = fechaHoy.AddDays(1);

                var montoDiario = await _context.TransaccionEnvio
                    .Where(t => t.TelefonoOrigen == request.TelefonoOrigen &&
                               t.FechaEnvio >= fechaHoy && t.FechaEnvio < fechaManana)
                    .SumAsync(t => t.Monto);

                if (montoDiario + request.Monto > 100000.00m)
                    return new TransaccionResponse(-1, "Límite diario excedido para este teléfono");

                // 6. Validar duplicidad (transacciones en últimos 5 minutos)
                var fechaLimite = DateTime.Now.AddMinutes(-5);
                var esDuplicada = await _context.TransaccionEnvio
                    .AnyAsync(t => t.TelefonoOrigen == request.TelefonoOrigen &&
                                  t.TelefonoDestino == request.TelefonoDestino &&
                                  t.Monto == request.Monto &&
                                  t.FechaEnvio >= fechaLimite);

                if (esDuplicada)
                    return new TransaccionResponse(-1, "Transacción duplicada detectada");

                // 7. Resolver transacción (HU SRV12)
                var respuestaResolucion = await ResolverTransaccionInternoAsync(request);

                // 8. Crear registro de transacción
                var transaccion = new TransaccionEnvio
                {
                    IdEntidadOrigen = entidadOrigen.IdEntidad,
                    IdEntidadDestino = entidadDestino.IdEntidad,
                    TelefonoOrigen = request.TelefonoOrigen,
                    NombreOrigen = request.NombreOrigen,
                    TelefonoDestino = request.TelefonoDestino,
                    Monto = request.Monto,
                    Descripcion = request.Descripcion,
                    CodigoRespuesta = respuestaResolucion.Codigo,
                    MensajeRespuesta = respuestaResolucion.Descripcion,
                    IdEstado = respuestaResolucion.Codigo == 0 ? 4 : 5, // 4 = Completado, 5 = Fallido
                    FechaEnvio = DateTime.Now
                };

                await _context.TransaccionEnvio.AddAsync(transaccion);
                await _context.SaveChangesAsync();

                // 9. Registrar bitácora asíncrona
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var bitacora = new Bitacora
                        {
                            Usuario = "Sistema",
                            Descripcion = "Recepción de transacción",
                            Detalle = $"Transacción {transaccion.IdTransaccion}: {request.TelefonoOrigen} -> {request.TelefonoDestino} - ${request.Monto}",
                            Tipo = respuestaResolucion.Codigo == 0 ? "INFO" : "ERROR",
                            Modulo = "SRV7",
                            Fecha = DateTime.Now
                        };

                        await _context.Bitacora.AddAsync(bitacora);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error en bitácora asíncrona: {ex.Message}");
                    }
                });

                // 10. Retornar respuesta según el resultado
                if (respuestaResolucion.Codigo == 0)
                {
                    return new TransaccionResponse(0, "Transacción aplicada");
                }
                else
                {
                    return new TransaccionResponse(-1, respuestaResolucion.Descripcion);
                }
            }
            catch (Exception ex)
            {
                await RegistrarErrorBitacoraAsync("SRV7_RecibirTransaccion", ex.Message, request);
                return new TransaccionResponse(-1, $"Error interno: {ex.Message}");
            }
        }

        // HU SRV8: Enviar transacciones (externas) - REQUIERE token
        public async Task<TransaccionResponse> EnviarTransaccionAsync(TransaccionRequest request, string token)
        {
            try
            {
                // 1. Validar token
                if (string.IsNullOrEmpty(token) || !await _authService.ValidateTokenAsync(token))
                    return new TransaccionResponse(-1, "No autorizado");

                // 2. Validar datos básicos
                if (!ValidarTransaccion(request))
                    return new TransaccionResponse(-1, "Debe enviar los datos completos y válidos");

                // 3. Validar que la entidad origen sea la del grupo (GRUPO01)
                var entidadOrigen = await _context.Entidades
                    .FirstOrDefaultAsync(e => e.NombreInstitucion == request.EntidadOrigen && e.IdEstado == 1);

                if (entidadOrigen == null)
                    return new TransaccionResponse(-1, "Solo puede enviar transacciones desde su propia entidad");

                // 4. Validar que la entidad destino exista
                var entidadDestino = await _context.Entidades
                    .FirstOrDefaultAsync(e => e.NombreInstitucion == request.EntidadDestino && e.IdEstado == 1);

                if (entidadDestino == null)
                    return new TransaccionResponse(-1, "Entidad destino no registrada");

                // 5. Validar que el teléfono origen esté afiliado
                var afiliacion = await _context.Afiliacion
                    .Include(a => a.IdEstadoNavigation)
                    .FirstOrDefaultAsync(a => a.Telefono == request.TelefonoOrigen && a.IdEstado == 1);

                if (afiliacion == null)
                    return new TransaccionResponse(-1, "Teléfono origen no afiliado a pagos móviles");

                // 6. Simular envío a servicio externo
                var respuestaExterna = await SimularEnvioExternoAsync(request);

                // 7. Crear registro de transacción
                var transaccion = new TransaccionEnvio
                {
                    IdEntidadOrigen = entidadOrigen.IdEntidad,
                    IdEntidadDestino = entidadDestino.IdEntidad,
                    TelefonoOrigen = request.TelefonoOrigen,
                    NombreOrigen = request.NombreOrigen,
                    TelefonoDestino = request.TelefonoDestino,
                    Monto = request.Monto,
                    Descripcion = request.Descripcion,
                    CodigoRespuesta = respuestaExterna.Codigo,
                    MensajeRespuesta = respuestaExterna.Descripcion,
                    IdEstado = respuestaExterna.Codigo == 0 ? 4 : 5,
                    FechaEnvio = DateTime.Now
                };

                await _context.TransaccionEnvio.AddAsync(transaccion);
                await _context.SaveChangesAsync();

                // 8. Registrar bitácora
                var bitacora = new Bitacora
                {
                    Usuario = "Sistema",
                    Descripcion = "Envío de transacción externa",
                    Detalle = $"Transacción {transaccion.IdTransaccion} enviada a {request.EntidadDestino}",
                    Tipo = "INFO",
                    Modulo = "SRV8",
                    Fecha = DateTime.Now
                };

                await _context.Bitacora.AddAsync(bitacora);
                await _context.SaveChangesAsync();

                // 9. Retornar misma respuesta que el servicio externo
                return respuestaExterna;
            }
            catch (Exception ex)
            {
                await RegistrarErrorBitacoraAsync("SRV8_EnviarTransaccion", ex.Message, request);
                return new TransaccionResponse(-1, $"Error interno: {ex.Message}");
            }
        }

        // Método interno para SRV12 (resolución de transacciones)
        private async Task<TransaccionResponse> ResolverTransaccionInternoAsync(TransaccionRequest request)
        {
            try
            {
                // 1. Validar que el teléfono origen esté afiliado
                var afiliacionOrigen = await _context.Afiliacion
                    .Include(a => a.IdEstadoNavigation)
                    .FirstOrDefaultAsync(a => a.Telefono == request.TelefonoOrigen && a.IdEstado == 1);

                if (afiliacionOrigen == null)
                    return new TransaccionResponse(-1, "Cliente no asociado a pagos móviles");

                // 2. Verificar si el teléfono destino está afiliado a nuestra entidad
                var afiliacionDestino = await _context.Afiliacion
                    .Include(a => a.IdEstadoNavigation)
                    .FirstOrDefaultAsync(a => a.Telefono == request.TelefonoDestino && a.IdEstado == 1);

                if (afiliacionDestino != null)
                {
                    // Transacción INTERNA
                    return await ProcesarTransaccionInternaAsync(request, afiliacionDestino);
                }
                else
                {
                    // Transacción EXTERNA
                    return await ProcesarTransaccionExternaAsync(request);
                }
            }
            catch (Exception ex)
            {
                await RegistrarErrorBitacoraAsync("ResolverTransaccionInterno", ex.Message, request);
                return new TransaccionResponse(-1, $"Error interno: {ex.Message}");
            }
        }

        // Método para transacciones internas (crédito)
        private async Task<TransaccionResponse> ProcesarTransaccionInternaAsync(TransaccionRequest request, Afiliacion afiliacionDestino)
        {
            try
            {
                // Simular aplicación de crédito en core bancario (HU SRV14)
                await Task.Delay(100); // Simular procesamiento

                // Registrar en bitácora
                var bitacora = new Bitacora
                {
                    Usuario = "Sistema",
                    Descripcion = "Transacción interna procesada",
                    Detalle = $"Crédito aplicado a {afiliacionDestino.Telefono} - Cuenta: {afiliacionDestino.NumeroCuenta} - Monto: ${request.Monto}",
                    Tipo = "INFO",
                    Modulo = "SRV12",
                    Fecha = DateTime.Now
                };

                await _context.Bitacora.AddAsync(bitacora);
                await _context.SaveChangesAsync();

                return new TransaccionResponse(0, "Transacción interna procesada exitosamente");
            }
            catch (Exception ex)
            {
                await RegistrarErrorBitacoraAsync("ProcesarTransaccionInterna", ex.Message, request);
                return new TransaccionResponse(-1, $"Error al procesar transacción interna: {ex.Message}");
            }
        }

        // Método para transacciones externas
        private async Task<TransaccionResponse> ProcesarTransaccionExternaAsync(TransaccionRequest request)
        {
            try
            {
                // Simular envío a servicio externo
                var response = await SimularEnvioExternoAsync(request);

                var bitacora = new Bitacora
                {
                    Usuario = "Sistema",
                    Descripcion = "Transacción externa enrutada",
                    Detalle = $"Transacción enrutada a {request.EntidadDestino} - Monto: ${request.Monto}",
                    Tipo = response.Codigo == 0 ? "INFO" : "ERROR",
                    Modulo = "SRV12",
                    Fecha = DateTime.Now
                };

                await _context.Bitacora.AddAsync(bitacora);
                await _context.SaveChangesAsync();

                return response;
            }
            catch (Exception ex)
            {
                await RegistrarErrorBitacoraAsync("ProcesarTransaccionExterna", ex.Message, request);
                return new TransaccionResponse(-1, $"Error al procesar transacción externa: {ex.Message}");
            }
        }

        // Simulación de envío externo
        private async Task<TransaccionResponse> SimularEnvioExternoAsync(TransaccionRequest request)
        {
            // Simulación de respuesta de servicio externo
            await Task.Delay(200); // Simular latencia de red

            // Reglas de simulación:
            if (request.Monto > 50000)
            {
                return new TransaccionResponse(-1, "Transacción pendiente de aprobación por monto elevado");
            }
            else if (request.Monto < 1)
            {
                return new TransaccionResponse(-1, "Monto mínimo no alcanzado");
            }
            else
            {
                // 80% de éxito, 20% de error
                var random = new Random();
                if (random.Next(0, 10) < 8)
                {
                    return new TransaccionResponse(0, "Transacción procesada exitosamente por entidad externa");
                }
                else
                {
                    return new TransaccionResponse(-1, "Error en procesamiento por entidad externa");
                }
            }
        }

        // HU SRV17: Reporte de transacciones diarias - REQUIERE token
        public async Task<ReporteDiarioResponse> GenerarReporteDiarioAsync(DateTime fecha, string token)
        {
            try
            {
                // 1. Validar token
                if (string.IsNullOrEmpty(token) || !await _authService.ValidateTokenAsync(token))
                    throw new UnauthorizedAccessException("No autorizado");

                var fechaReporte = fecha.Date;
                var fechaFin = fechaReporte.AddDays(1);

                // 2. Obtener transacciones del día
                var transacciones = await _context.TransaccionEnvio
                    .Include(t => t.IdEntidadDestinoNavigation)
                    .Include(t => t.IdEntidadOrigenNavigation)
                    .Include(t => t.IdEstadoNavigation)
                    .Where(t => t.FechaEnvio >= fechaReporte && t.FechaEnvio < fechaFin)
                    .OrderByDescending(t => t.FechaEnvio)
                    .ToListAsync();

                // 3. Calcular estadísticas
                var totalTransacciones = transacciones.Count;
                var totalMonto = transacciones.Sum(t => t.Monto);
                var transaccionesExitosas = transacciones.Count(t => t.IdEstado == 4); // 4 = Completado
                var transaccionesFallidas = transacciones.Count(t => t.IdEstado == 5); // 5 = Fallido
                var montoPromedio = totalTransacciones > 0 ? totalMonto / totalTransacciones : 0;

                // 4. Construir respuesta
                var reporte = new ReporteDiarioResponse
                {
                    Fecha = fechaReporte,
                    TotalTransacciones = totalTransacciones,
                    TotalMonto = totalMonto,
                    TransaccionesExitosas = transaccionesExitosas,
                    TransaccionesFallidas = transaccionesFallidas,
                    MontoPromedio = montoPromedio,
                    Detalles = transacciones.Select(t => new TransaccionDetalle
                    {
                        TransaccionId = t.IdTransaccion,
                        TelefonoOrigen = t.TelefonoOrigen,
                        TelefonoDestino = t.TelefonoDestino,
                        Monto = t.Monto,
                        Descripcion = t.Descripcion,
                        Estado = t.IdEstadoNavigation?.Nombre ?? "Desconocido",
                        Fecha = t.FechaEnvio ?? DateTime.Now,
                        EntidadOrigen = t.IdEntidadOrigenNavigation?.NombreInstitucion ?? "Desconocida",
                        EntidadDestino = t.IdEntidadDestinoNavigation?.NombreInstitucion ?? "Desconocida"
                    }).ToList()
                };

                // 5. Registrar bitácora
                var bitacora = new Bitacora
                {
                    Usuario = "Sistema",
                    Descripcion = "Generación de reporte diario",
                    Detalle = $"Reporte {fechaReporte:yyyy-MM-dd}: {totalTransacciones} transacciones, Total: ${totalMonto}",
                    Tipo = "INFO",
                    Modulo = "SRV17",
                    Fecha = DateTime.Now
                };

                await _context.Bitacora.AddAsync(bitacora);
                await _context.SaveChangesAsync();

                return reporte;
            }
            catch (Exception ex)
            {
                await RegistrarErrorBitacoraAsync("SRV17_GenerarReporteDiario", ex.Message, null);
                throw;
            }
        }

        // Método auxiliar para registrar errores
        private async Task RegistrarErrorBitacoraAsync(string metodo, string error, TransaccionRequest? request)
        {
            try
            {
                var bitacora = new Bitacora
                {
                    Usuario = "Sistema",
                    Descripcion = $"Error en {metodo}",
                    Detalle = $"Error: {error}. Request: {(request != null ? System.Text.Json.JsonSerializer.Serialize(request) : "null")}",
                    Tipo = "ERROR",
                    Modulo = "Transacciones",
                    Fecha = DateTime.Now
                };

                await _context.Bitacora.AddAsync(bitacora);
                await _context.SaveChangesAsync();
            }
            catch
            {
                Console.WriteLine($"Error en {metodo}: {error}");
            }
        }
    }
}