using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class AfiliacionRepository : IAfiliacionRepository
    {
        private readonly PagosMovilesContext _context;
        private readonly ILogger<AfiliacionRepository> _logger;

        public AfiliacionRepository(PagosMovilesContext context, ILogger<AfiliacionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Afiliacion?> GetByTelefonoAndIdentificacionAsync(string telefono, string identificacion)
        {
            try
            {
                return await _context.Afiliacion
                    .FirstOrDefaultAsync(a =>
                        a.Telefono == telefono &&
                        a.IdentificacionUsuario == identificacion &&
                        a.IdEstado == 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error obteniendo afiliación por teléfono {Telefono} e identificación {Identificacion}",
                    telefono, identificacion);
                throw;
            }
        }

        public async Task<Afiliacion?> ObtenerPorTelefonoAsync(string telefono)
        {
            try
            {
                return await _context.Afiliacion
                    .FirstOrDefaultAsync(a => a.Telefono == telefono && a.IdEstado == 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo afiliación por teléfono {Telefono}", telefono);
                throw;
            }
        }

        public async Task<Entidades?> ObtenerEntidadPorIdAsync(int id)
        {
            try
            {
                string idString = id.ToString();  // ← Convertir int a string

                return await _context.Entidades
                    .FirstOrDefaultAsync(e => e.IdEntidad == idString && e.IdEstado == 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo entidad por ID {Id}", id);
                throw;
            }
        }
    }
}