using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class TransaccionRepository
    {
        private readonly PagosMovilesContext _context;
        private readonly ILogger<TransaccionRepository> _logger;

        public TransaccionRepository(PagosMovilesContext context, ILogger<TransaccionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TransaccionEnvio>> ObtenerPorFechaYEntidadAsync(
            DateTime fechaInicio,
            DateTime fechaFin,
            int? entidadId = null)
        {
            try
            {
                var query = _context.TransaccionEnvio
                    .Where(t => t.FechaEnvio >= fechaInicio && t.FechaEnvio <= fechaFin);

                if (entidadId.HasValue)
                {
                    query = query.Where(t =>
                        t.IdEntidadOrigen == entidadId ||
                        t.IdEntidadDestino == entidadId);
                }

                return await query
                    .OrderByDescending(t => t.FechaEnvio)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo transacciones por fecha");
                throw;
            }
        }
    }
}