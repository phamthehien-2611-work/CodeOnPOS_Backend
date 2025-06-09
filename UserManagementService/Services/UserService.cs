using System.Net;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Protobuf.WellKnownTypes;
using UserManagementService.Protos;
using UserManagementService.Infrastructure.Persistence;
using UserManagementService.Domain.Entities;

namespace UserManagementService.Services
{
    public class UserService : UserAuthService.UserAuthServiceBase
    {
        private readonly UserDbContext _context;
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration, UserDbContext context)
        {
            _context = context;
            _configuration = configuration;
        }

        public override async Task<ApiResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            bool existingUser = _context.tbl_Users.Any(u => u.UserName == request.UserName);
            if (existingUser)
            {
                return new ApiResponse { StatusCode = (int)HttpStatusCode.BadRequest, MessageText = "Tài khoản đã tồn tại." };
            }

            // Băm mật khẩu trước khi lưu
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            tbl_User user = new tbl_User
            {
                UserId = Guid.NewGuid(),
                UserName = request.UserName,
                PasswordHash = hashedPassword, // Chỉ lưu mật khẩu đã mã hóa
                FullName = request.UserName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                Address = request.Address,
                CreatedTime = DateTime.UtcNow,
            };

            _context.tbl_Users.Add(user);
            await _context.SaveChangesAsync();

            return new ApiResponse { StatusCode = (int)HttpStatusCode.OK, MessageText = "Đăng ký thành công." };
        }

        public override async Task<ApiResponse> Login(LoginRequest request, ServerCallContext context)
        {
            tbl_User user = _context.tbl_Users.FirstOrDefault(u => u.UserName == request.UserName);

            // Giả lập kiểm tra mật khẩu
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new ApiResponse { StatusCode = (int)HttpStatusCode.BadRequest, MessageText = "Tên đăng nhập hoặc mật khẩu không chính xác." };
            }

            // Nếu thông tin chính xác, tạo JWT token
            string token = GenerateJwtToken(user);
            var loginPayload = new LoginSuccessPayload { Token = token };

            return new ApiResponse { StatusCode = (int)HttpStatusCode.OK, MessageText = "Đăng nhập thành công.", Data = Any.Pack(loginPayload) };
        }

        private string GenerateJwtToken(dynamic user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim("UserId", user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                // Bạn có thể thêm các claims khác như vai trò (role) ở đây
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}