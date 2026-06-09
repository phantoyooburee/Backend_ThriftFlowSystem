using Backend_ThriftFlowSystem.Interfaces;
using Backend_ThriftFlowSystem.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Backend_ThriftFlowSystem.Utils
{
    public class GetTokenJWT : ITokenServices
    {
        private readonly IConfiguration _config;

        public GetTokenJWT(IConfiguration config)
        {
            _config = config;
        }

        // Create a JWT token for the authenticated user
        public string GenerateJwtToken(Employee employee)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, employee.Username),
                new Claim(ClaimTypes.Email, employee.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if(employee.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, employee.Role.RoleName));
            }

            var expiresMinutes = double.Parse(
                _config["Jwt:ExpireMinutes"] ?? "480");

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Create Random Token for Reset Password
        public string GenerateResetPasswordToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToHexString(bytes);
        }

        // Hash Token before saving to database
        public string SHA256Hex(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
