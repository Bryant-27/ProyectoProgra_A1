using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class BitacoraRepository
    {
        private readonly CoreBancarioContext _context;
        private readonly ILogger<BitacoraRepository> _logger;

        public BitacoraRepository(CoreBancarioContext context, ILogger<BitacoraRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Bitacora> RegistrarAsync(Bitacora bitacora)
        {
            try
            {
                bitacora.FechaRegistro = DateTime.Now;
                await _context.Bitacoras.AddAsync(bitacora);
                await _context.SaveChangesAsync();
                return bitacora;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando bitácora");
                throw;
            }
        }

        public async Task<List<Bitacora>> ListarAsync(
            string usuario = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            string accion = null,
            string resultado = null,
            int limite = 100)
        {
            var query = _context.Bitacoras.AsQueryable();

            if (!string.IsNullOrWhiteSpace(usuario))
                query = query.Where(b => b.Usuario.Contains(usuario));

            if (fechaInicio.HasValue)
                query = query.Where(b => b.FechaRegistro >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(b => b.FechaRegistro <= fechaFin.Value);

            if (!string.IsNullOrWhiteSpace(accion))
                query = query.Where(b => b.Accion == accion);

            if (!string.IsNullOrWhiteSpace(resultado))
                query = query.Where(b => b.Resultado == resultado);

            return await query
                .OrderByDescending(b => b.FechaRegistro)
                .Take(limite)
                .ToListAsync();
        }
    }
}