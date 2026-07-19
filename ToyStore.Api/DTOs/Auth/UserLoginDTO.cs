namespace ToyStore.Api.DTOs.Auth
{
    /// <summary>
    /// USER INBOUND LOGIN DTO
    /// </summary>
    public class UserLoginDTO
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}