using DataAccess.Models;
using Entities.DTOs;  // ← Usar los DTOs de Entities
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Logica_Negocio.Services
{
    public interface IBitacoraConsultaService
    {
        Task<BitacoraPaginadaDto> ObtenerBitacorasAsync(BitacoraFiltrosDto filtros, int pagina, int tamanoPagina);
        Task<BitacoraResponseDto> ObtenerPorIdAsync(long id);
        Task<EstadisticasBitacoraDto> ObtenerEstadisticasAsync(DateTime? fechaDesde, DateTime? fechaHasta);
    }

    public class BitacoraConsultaService : IBitacoraConsultaService
    {
        private readonly DBContext_Bitacora _context;

        public BitacoraConsultaService(DBContext_Bitacora context)
        {
            _context = context;
        }

        public async Task<BitacoraPaginadaDto> ObtenerBitacorasAsync(BitacoraFiltrosDto filtros, int pagina, int tamanoPagina)
        {
            var query = _context.Bitacora.AsQueryable();

            // Aplicar filtros
            if (filtros.FechaDesde.HasValue)
                query = query.Where(b => b.FechaRegistro >= filtros.FechaDesde.Value);

            if (filtros.FechaHasta.HasValue)
                query = query.Where(b => b.FechaRegistro <= filtros.FechaHasta.Value);

            if (!string.IsNullOrWhiteSpace(filtros.Usuario))
                query = query.Where(b => b.Usuario.Contains(filtros.Usuario));

            if (!string.IsNullOrWhiteSpace(filtros.Accion))
                query = query.Where(b => b.Accion.Contains(filtros.Accion));

            if (!string.IsNullOrWhiteSpace(filtros.Servicio))
                query = query.Where(b => b.Servicio.Contains(filtros.Servicio));

            if (!string.IsNullOrWhiteSpace(filtros.Resultado))
                query = query.Where(b => b.Resultado == filtros.Resultado);

            // Ordenar por fecha descendente
            query = query.OrderByDescending(b => b.FechaRegistro);

            var totalRegistros = await query.CountAsync();

            var bitacoras = await query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .Select(b => new BitacoraResponseDto
                {
                    BitacoraId = b.BitacoraId,
                    Usuario = b.Usuario,
                    Accion = b.Accion,
                    Descripcion = b.Descripcion,
                    FechaRegistro = b.FechaRegistro,
                    Servicio = b.Servicio,
                    Resultado = b.Resultado
                })
                .ToListAsync();

            return new BitacoraPaginadaDto
            {
                Bitacoras = bitacoras,
                TotalRegistros = totalRegistros
            };
        }

        public async Task<BitacoraResponseDto> ObtenerPorIdAsync(long id)
        {
            var bitacora = await _context.Bitacora.FindAsync(id);

            if (bitacora == null) return null;

            return new BitacoraResponseDto
            {
                BitacoraId = bitacora.BitacoraId,
                Usuario = bitacora.Usuario,
                Accion = bitacora.Accion,
                Descripcion = bitacora.Descripcion,
                FechaRegistro = bitacora.FechaRegistro,
                Servicio = bitacora.Servicio,
                Resultado = bitacora.Resultado
            };
        }

        public async Task<EstadisticasBitacoraDto> ObtenerEstadisticasAsync(DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var query = _context.Bitacora.AsQueryable();

            if (fechaDesde.HasValue)
                query = query.Where(b => b.FechaRegistro >= fechaDesde.Value);
            if (fechaHasta.HasValue)
                query = query.Where(b => b.FechaRegistro <= fechaHasta.Value);

            var total = await query.CountAsync();
            var exitosos = await query.CountAsync(b => b.Resultado == "EXITO");
            var errores = await query.CountAsync(b => b.Resultado == "ERROR");

            // CORRECCIÓN: Materializar antes del Select con ToList()
            var accionesTop = await query
                .GroupBy(b => b.Accion)
                .Select(g => new { Accion = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToListAsync();

            var serviciosTop = await query
                .GroupBy(b => b.Servicio)
                .Select(g => new { Servicio = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToListAsync();

            return new EstadisticasBitacoraDto
            {
                TotalRegistros = total,
                TotalExitosos = exitosos,
                TotalErrores = errores,
                AccionesMasFrecuentes = accionesTop.ToDictionary(x => x.Accion, x => x.Cantidad),
                ServiciosMasUsados = serviciosTop.ToDictionary(x => x.Servicio, x => x.Cantidad)
            };
        }
    }
}
