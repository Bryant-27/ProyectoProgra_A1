// using Abstract.Repositories;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;
// using Models.PagosMoviles;
// using DataAccess.Context;

// namespace DataAccess.Repositories
// {
//     public class EntidadRepository // : IEntidadRepository
//     {
//         private readonly PagosMovilesContext _context;
//         private readonly ILogger<EntidadRepository> _logger;

//         public EntidadRepository(PagosMovilesContext context, ILogger<EntidadRepository> logger)
//         {
//             _context = context;
//             _logger = logger;
//         }

//         public async Task<bool> ExistsAndIsActiveAsync(int entidadId)
//         {
//             try
//             {
//                 var entidad = await _context.Entidades
//                     .FirstOrDefaultAsync(e => e.ID_Entidad == entidadId);

//                 return entidad != null && entidad.ID_Estado == 1;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error verificando entidad {EntidadId}", entidadId);
//                 throw;
//             }
//         }
//     }
// }