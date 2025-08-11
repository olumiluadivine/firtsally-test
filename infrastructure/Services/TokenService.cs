using application.Contracts.Services;
using domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace infrastructure.Services;

internal class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly int _jwtExp;
    private readonly string _jwtIssuer;
    private readonly string _jwtKey;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _jwtExp = int.Parse(_configuration["JWT:ExpirationDays"] ?? "30");
        _jwtKey = _configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        _jwtIssuer = _configuration["JWT:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
    }

    public string CreateToken(User user)
    {
        try
        {
            var userClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, string.Join(",", user.Accounts.Select(x => x.AccountType.ToString())))
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creadentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(userClaims),
                Expires = DateTime.UtcNow.AddMonths(_jwtExp),
                SigningCredentials = creadentials,
                Issuer = _jwtIssuer,
                Audience = _jwtIssuer
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(jwt);

            return token;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}