using System.Text.Json.Serialization;

namespace Servicios.DTOs
{
    public class LoginRequest
    {
        [JsonPropertyName("idUsuario")]
        public int IdUsuario { get; set; }

        [JsonPropertyName("usuario")]
        public string Usuario { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")]
        public string Contrasena { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        [JsonPropertyName("expires_in")]
        public DateTime ExpiresIn { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("usuarioID")]
        public int UsuarioID { get; set; }
    }
}