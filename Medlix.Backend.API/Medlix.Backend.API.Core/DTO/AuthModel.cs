namespace Medlix.Backend.API.Core.DTO
{
    public class AuthUser
    {
        public bool IsAuthenticated { get; set; }
        public string? PatientId { get; set; }
        public string? Role { get; set; }
        public string? ValidJwtToken { get; set; }

        public JwtTokenStatus JwtTokenStatus { get; set; }
        public string? ErrorMessage { get; set; }

        public bool IsB2CToken { get; set; }
    }

    public class AuthResult
    {
        public string? JWTAccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? Message { get; set; }
        public int? StatusCode { get; set; }
    }

    public enum JwtTokenStatus
    {
        Valid,
        Expired,
        Error,
        NoToken
    }
}
