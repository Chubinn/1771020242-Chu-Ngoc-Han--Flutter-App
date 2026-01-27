using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PcmApi.Data;
using PcmApi.Dtos;
using PcmApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PcmApi.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<UserDto?> GetCurrentUserAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly PcmDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, 
            PcmDbContext context, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new AuthResponse { Success = false, Message = "User already exists" };
                }

                var user = new IdentityUser
                {
                    Email = request.Email,
                    UserName = request.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return new AuthResponse { Success = false, Message = string.Join(", ", result.Errors.Select(e => e.Description)) };
                }

                // Assign Member role
                await _userManager.AddToRoleAsync(user, UserRoles.Member);

                // Create member profile
                var member = new Member
                {
                    UserId = user.Id,
                    FullName = request.FullName,
                    JoinDate = DateTime.UtcNow,
                    WalletBalance = 0,
                    Tier = MemberTier.Standard
                };

                _context.Members.Add(member);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = await GenerateJwtTokenAsync(user);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = token,
                    User = new UserDto
                    {
                        MemberId = member.Id,
                        UserId = user.Id,
                        Email = user.Email!,
                        FullName = member.FullName,
                        WalletBalance = member.WalletBalance,
                        Tier = member.Tier.ToString(),
                        RankLevel = member.RankLevel
                    }
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return new AuthResponse { Success = false, Message = "Invalid credentials" };
                }

                var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordValid)
                {
                    return new AuthResponse { Success = false, Message = "Invalid credentials" };
                }

                var member = _context.Members.FirstOrDefault(m => m.UserId == user.Id);
                if (member == null)
                {
                    return new AuthResponse { Success = false, Message = "Member profile not found" };
                }

                var token = await GenerateJwtTokenAsync(user);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new UserDto
                    {
                        MemberId = member.Id,
                        UserId = user.Id,
                        Email = user.Email!,
                        FullName = member.FullName,
                        WalletBalance = member.WalletBalance,
                        Tier = member.Tier.ToString(),
                        RankLevel = member.RankLevel,
                        AvatarUrl = member.AvatarUrl
                    }
                };
            }
            catch (Exception ex)
            {
                return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<UserDto?> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            var member = _context.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null)
                return null;

            return new UserDto
            {
                MemberId = member.Id,
                UserId = user.Id,
                Email = user.Email!,
                FullName = member.FullName,
                WalletBalance = member.WalletBalance,
                Tier = member.Tier.ToString(),
                RankLevel = member.RankLevel,
                AvatarUrl = member.AvatarUrl
            };
        }

        private async Task<string> GenerateJwtTokenAsync(IdentityUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);
            var memberId = _context.Members
                .Where(m => m.UserId == user.Id)
                .Select(m => m.Id)
                .FirstOrDefault();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim("MemberId", memberId.ToString())
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"]!)),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
