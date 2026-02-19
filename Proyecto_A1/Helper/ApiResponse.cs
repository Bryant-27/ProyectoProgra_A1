namespace Proyecto_A1.Helper
{
    public class ApiResponse<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = "";
        public T? Datos { get; set; }
        public List<string>? Errores { get; set; }

        public static IResult Success(T data, string mensaje = "Operación exitosa")
            => Results.Ok(new ApiResponse<T>
            {
                Exito = true,
                Mensaje = mensaje,
                Datos = data
            });

        public static IResult Error(List<string> errores, string mensaje = "Error de validación")
            => Results.BadRequest(new ApiResponse<T>
            {
                Exito = false,
                Mensaje = mensaje,
                Errores = errores
            });

        public static IResult NotFound(string mensaje)
            => Results.NotFound(new ApiResponse<T>
            {
                Exito = false,
                Mensaje = mensaje
            });
    }
}
