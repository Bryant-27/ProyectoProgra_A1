using System.ComponentModel.DataAnnotations;

namespace Proyecto_A1.Helper
{
    public class ValidationHelper
    {
        public static List<string> ValidarModelo(object modelo)
        {
            var context = new ValidationContext(modelo);
            var resultados = new List<ValidationResult>();

            Validator.TryValidateObject(modelo, context, resultados, true);

            return resultados
                .Select(r => r.ErrorMessage ?? "Error de validación")
                .ToList();
        }

        public static bool EsVacio(string? valor)
            => string.IsNullOrWhiteSpace(valor);
    }
}
