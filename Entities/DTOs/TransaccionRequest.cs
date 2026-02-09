using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs
{
    public class TransaccionRequest
    {
        [Required(ErrorMessage = "La entidad origen es requerida")]
        public string EntidadOrigen { get; set; } = null!;

        [Required(ErrorMessage = "La entidad destino es requerida")]
        public string EntidadDestino { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono origen es requerido")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Formato de teléfono inválido")]
        public string TelefonoOrigen { get; set; } = null!;

        [Required(ErrorMessage = "El nombre origen es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres")]
        public string NombreOrigen { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono destino es requerido")]
        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Formato de teléfono inválido")]
        public string TelefonoDestino { get; set; } = null!;

        [Required(ErrorMessage = "El monto es requerido")]
        [Range(0.01, 100000.00, ErrorMessage = "El monto debe estar entre 0.01 y 100,000.00")]
        public decimal Monto { get; set; }

        [StringLength(25, ErrorMessage = "La descripción no puede superar 25 caracteres")]
        public string? Descripcion { get; set; }
    }
}