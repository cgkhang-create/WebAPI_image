using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI_image.Models.DTO;
using WebAPI_image.Repositories;

namespace WebAPI_image.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenRepository _tokenRepository;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ITokenRepository tokenRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenRepository = tokenRepository;
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = request.Username?.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Vui lòng nhập tên đăng nhập và mật khẩu.");

            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null)
                return BadRequest("Tên đăng nhập đã tồn tại.");

            var requestedRole = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role.Trim();

            if (requestedRole != "User" && requestedRole != "Admin")
                return BadRequest("Role chỉ được phép là 'User' hoặc 'Admin'.");

            // Chỉ Admin mới được tạo tài khoản Admin
            if (requestedRole == "Admin")
            {
                if (User.Identity?.IsAuthenticated != true)
                    return Unauthorized("Chỉ Admin mới được tạo tài khoản Admin. Vui lòng đăng nhập trước.");

                if (!User.IsInRole("Admin"))
                    return Forbid("Bạn không có quyền tạo tài khoản Admin.");
            }

            var user = new IdentityUser
            {
                UserName = username,
                Email = username
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new
                {
                    Message = "Đăng ký thất bại.",
                    Errors = createResult.Errors.Select(e => e.Description)
                });
            }

            if (!await _roleManager.RoleExistsAsync(requestedRole))
            {
                await _userManager.DeleteAsync(user);
                return StatusCode(500, new { Message = $"Role {requestedRole} chưa tồn tại." });
            }

            var roleResult = await _userManager.AddToRoleAsync(user, requestedRole);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return StatusCode(500, new
                {
                    Message = $"Không thể gán quyền {requestedRole}.",
                    Errors = roleResult.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                Message = "Đăng ký thành công.",
                Username = username,
                Role = requestedRole
            });
        }

        /// <summary>
        /// Đăng nhập
        /// </summary>
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Vui lòng nhập tên đăng nhập và mật khẩu.");

            var user = await _userManager.FindByNameAsync(request.Username.Trim());
            if (user == null)
                return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng.");

            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: false);

            if (!signInResult.Succeeded)
                return Unauthorized("Tên đăng nhập hoặc mật khẩu không đúng.");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenRepository.CreateJWTToken(user, roles.ToList());

            return Ok(new LoginResponseDTO
            {
                JwtToken = token
            });
        }

        /// <summary>
        /// Lấy thông tin user đang đăng nhập
        /// </summary>
        [HttpGet("Me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.FindFirstValue(ClaimTypes.Name);
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new
            {
                UserId = userId,
                Username = username,
                Roles = roles
            });
        }
    }
}