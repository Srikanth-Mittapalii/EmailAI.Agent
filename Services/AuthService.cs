using EmailAI.Agent.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EmailAI.Agent.Services
{
    public class AuthService
    {
        private readonly IMongoCollection<User> _users;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AuthService(IMongoDatabase database, IConfiguration config)
        {
            _users = database.GetCollection<User>("Users");
            _jwtKey = config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key missing");
            _jwtIssuer = config["Jwt:Issuer"] ?? "EmailAIAgent";
            _jwtAudience = config["Jwt:Audience"] ?? "EmailAIUsers";
            
            // Create unique index for email
            var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(u => u.Email);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<User>(indexKeysDefinition, indexOptions);
            _users.Indexes.CreateOne(indexModel);
        }

        public async Task<User?> Signup(string email, string password)
        {
            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _users.InsertOneAsync(user);
                return user;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return null; // User already exists
            }
        }

        public async Task<string?> Login(string email, string password)
        {
            var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null || user.PasswordHash == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null;
            }

            return GenerateJwtToken(user);
        }

        public async Task<string?> ValidateGoogleToken(string accessToken)
        {
            try
            {
                // Fetch User Info using Access Token
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                
                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                var googleUser = System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (googleUser == null) return null;

                var user = await _users.Find(u => u.Email == googleUser.Email).FirstOrDefaultAsync();
                if (user == null)
                {
                    user = new User
                    {
                        Email = googleUser.Email,
                        Name = googleUser.Name,
                        GoogleId = googleUser.Sub,
                        ProfilePicture = googleUser.Picture,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _users.InsertOneAsync(user);
                }
                else if (string.IsNullOrEmpty(user.GoogleId))
                {
                    // Link existing account
                    var update = Builders<User>.Update
                        .Set(u => u.GoogleId, googleUser.Sub)
                        .Set(u => u.Name, googleUser.Name)
                        .Set(u => u.ProfilePicture, googleUser.Picture);
                    await _users.UpdateOneAsync(u => u.Id == user.Id, update);
                }

                // After successful login, start background sync for Gmail (last 100 emails)
                // Passing the accessToken to the sync service
                // _syncService.SyncGmail(user.Id, accessToken); // Todo: Implement SyncService

                return GenerateJwtToken(user);
            }
            catch
            {
                return null;
            }
        }

        private class GoogleUserInfo
        {
            public string Sub { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string Picture { get; set; } = null!;
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
