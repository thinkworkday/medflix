using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace UnitTesting.Utils
{
    public class UtilsHelper
    {
        public static string GenerateToken(DateTime expireDuration, string patientId = "", string? myIssuer = null, string myAudience = "", string secretKey = "")
        {
            string userId = Environment.GetEnvironmentVariable("UserId");
            myIssuer = string.IsNullOrEmpty(myIssuer) ? Environment.GetEnvironmentVariable("IssUser") : myIssuer;
            secretKey = string.IsNullOrEmpty(secretKey) ? Environment.GetEnvironmentVariable("JwtSecretKey") : secretKey;
            myAudience = string.IsNullOrEmpty(myAudience) ? Environment.GetEnvironmentVariable("Audience") : myAudience;
            var mySecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    string.IsNullOrEmpty(patientId) ? new Claim("role", "admin") : new Claim("patient_id", patientId),
                    new Claim("user_id", userId),
                }),
                Expires = expireDuration,
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
