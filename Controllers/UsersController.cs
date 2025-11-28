using Microsoft.AspNetCore.Mvc;
using ProjectCms.Models;
using ProjectCms.Services;

namespace ProjectCms.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<List<UserResponseDto>>> GetAll()
        {
            var users = await _userService.GetAsync();
            var userDtos = users.Select(UserService.ToDto).ToList();
            return Ok(userDtos);
        }

        // GET: api/Users/{id}
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<UserResponseDto>> GetById(string id)
        {
            var user = await _userService.GetAsync(id);

            if (user is null)
                return NotFound(new { message = "User not found" });

            return Ok(UserService.ToDto(user));
        }

        // GET: api/Users/username/{username}
        [HttpGet("username/{username}")]
        public async Task<ActionResult<UserResponseDto>> GetByUsername(string username)
        {
            var user = await _userService.GetByUsernameAsync(username);

            if (user is null)
                return NotFound(new { message = "User not found" });

            return Ok(UserService.ToDto(user));
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> Create([FromBody] CreateUserDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Username))
                return BadRequest(new { message = "Username is required" });

            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Email is required" });

            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Password is required" });

            // Check if username already exists
            var existingUser = await _userService.GetByUsernameAsync(dto.Username);
            if (existingUser != null)
                return BadRequest(new { message = "Username already exists" });

            // Check if email already exists
            var existingEmail = await _userService.GetByEmailAsync(dto.Email);
            if (existingEmail != null)
                return BadRequest(new { message = "Email already exists" });

            var user = await _userService.CreateAsync(dto);
            var userDto = UserService.ToDto(user);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, userDto);
        }

        // PUT: api/Users/{id}
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, [FromBody] User updatedUser)
        {
            var existing = await _userService.GetAsync(id);
            if (existing is null)
                return NotFound(new { message = "User not found" });

            updatedUser.Id = id;
            updatedUser.CreatedAt = existing.CreatedAt; // Preserve creation date
            updatedUser.PasswordHash = existing.PasswordHash; // Don't update password here

            await _userService.UpdateAsync(id, updatedUser);
            return NoContent();
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _userService.GetAsync(id);
            if (existing is null)
                return NotFound(new { message = "User not found" });

            await _userService.RemoveAsync(id);
            return NoContent();
        }

        // POST: api/Users/login
        [HttpPost("login")]
        public async Task<ActionResult<UserResponseDto>> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Username and password are required" });

            var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);

            if (user is null)
                return Unauthorized(new { message = "Invalid username or password" });

            return Ok(UserService.ToDto(user));
        }

        // POST: api/Users/{id}/change-password
        [HttpPost("{id:length(24)}/change-password")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordDto dto)
        {
            var existing = await _userService.GetAsync(id);
            if (existing is null)
                return NotFound(new { message = "User not found" });

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "New password is required" });

            await _userService.ChangePasswordAsync(id, dto.NewPassword);
            return NoContent();
        }
    }
}