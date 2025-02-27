using Microsoft.IdentityModel.Tokens;
using ParkingBooking.Web.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ParkingBooking.Web.Services
{
    public class JWTService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryInMinutes;

        public JWTService(IConfiguration configuration)
        {
            // Получаем настройки JWT из конфигурации
            _secretKey = configuration["JwtSettings:Secret"];
            _issuer = configuration["JwtSettings:Issuer"];
            _audience = configuration["JwtSettings:Audience"];
            _expiryInMinutes = int.Parse(configuration["JwtSettings:ExpiryInMinutes"]);
        }

        public string GenerateToken(User user)
        {
            // Создаем claims (утверждения) для токена
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role) // Добавляем роль пользователя
            };

            // Создаем ключ для подписи токена
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Создаем JWT-токен
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryInMinutes),
                signingCredentials: credentials
            );

            // Сериализуем токен в строку
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }
    }
}
