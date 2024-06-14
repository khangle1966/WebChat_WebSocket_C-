public class User
{
    public string Username { get; set; } = string.Empty; // Đảm bảo không null
    public string Password { get; set; } = string.Empty; // Đảm bảo không null
    public string PasswordHash { get; set; } = string.Empty; // Đảm bảo không null
    public string PasswordSalt { get; set; } = string.Empty; // Đảm bảo không null
    public string Email { get; set; } = string.Empty; // Đảm bảo không null
}
