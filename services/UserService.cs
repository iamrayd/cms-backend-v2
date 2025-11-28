using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProjectCms.Models;
using System.Security.Cryptography;
using System.Text;

namespace ProjectCms.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IOptions<MongoDbSettings> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            var database = client.GetDatabase(options.Value.DatabaseName);
            _users = database.GetCollection<User>("Users");
        }

        // GET: All users
        public async Task<List<User>> GetAsync() =>
            await _users.Find(_ => true).ToListAsync();

        // GET: User by ID
        public async Task<User?> GetAsync(string id) =>
            await _users.Find(x => x.Id == id).FirstOrDefaultAsync();

        // GET: User by username
        public async Task<User?> GetByUsernameAsync(string username) =>
            await _users.Find(x => x.Username == username).FirstOrDefaultAsync();

        // GET: User by email
        public async Task<User?> GetByEmailAsync(string email) =>
            await _users.Find(x => x.Email == email).FirstOrDefaultAsync();

        // CREATE: New user
        public async Task<User> CreateAsync(CreateUserDto dto)
        {
            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Role = dto.Role,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _users.InsertOneAsync(user);
            return user;
        }

        // UPDATE: User
        public async Task UpdateAsync(string id, User updatedUser) =>
            await _users.ReplaceOneAsync(x => x.Id == id, updatedUser);

        // DELETE: User
        public async Task RemoveAsync(string id) =>
            await _users.DeleteOneAsync(x => x.Id == id);

        // LOGIN: Authenticate user
        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await GetByEmailAsync(email);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return null;

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await UpdateAsync(user.Id, user);

            return user;
        }

        // HELPER: Hash password
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // HELPER: Verify password
        private bool VerifyPassword(string password, string passwordHash)
        {
            var hash = HashPassword(password);
            return hash == passwordHash;
        }

        // HELPER: Convert User to UserResponseDto
        public static UserResponseDto ToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };
        }

        // CHANGE PASSWORD: Update password only
        public async Task ChangePasswordAsync(string id, string newPassword)
        {
            var user = await GetAsync(id);
            if (user == null) return;

            user.PasswordHash = HashPassword(newPassword);
            await UpdateAsync(id, user);
        }
    }
}