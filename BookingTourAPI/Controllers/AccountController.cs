// File: Controllers/AccountController.cs
using BookingTourAPI.Models;
using BookingTourAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _config = config;
            _emailSender = emailSender;
        }

        // ================== ĐĂNG KÝ (BẮT BUỘC XÁC NHẬN EMAIL) ==================
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ." });
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                if (existingUser.EmailConfirmed)
                {
                    // Đã có tài khoản & đã kích hoạt
                    return BadRequest(new
                    {
                        message = "Email này đã được sử dụng. Vui lòng dùng email khác hoặc đăng nhập."
                    });
                }
                else
                {
                    // Đã đăng ký nhưng chưa kích hoạt → gửi lại mail xác nhận
                    await SendConfirmationEmail(existingUser);
                    return Ok(new
                    {
                        message = "Email này đã đăng ký nhưng chưa kích hoạt. Hệ thống đã gửi lại email xác nhận, vui lòng kiểm tra hộp thư."
                    });
                }
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(new
                {
                    message = "Không thể tạo tài khoản.",
                    errors
                });
            }

            // Gửi email xác nhận
            await SendConfirmationEmail(user);

            return Ok(new
            {
                message = "Đăng ký thành công. Vui lòng kiểm tra email để kích hoạt tài khoản trước khi đăng nhập."
            });
        }

        // Hàm dùng chung để gửi email xác nhận
        private async Task SendConfirmationEmail(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Encode token an toàn qua URL
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);

            var confirmUrl =
                $"{Request.Scheme}://{Request.Host}/api/account/confirm-email?userId={user.Id}&token={encodedToken}";

            string subject = "[BookingTour] Xác nhận đăng ký tài khoản";
            string body = $@"
                <h3>Chào mừng đến với BookingTour!</h3>
                <p>Bạn đã đăng ký tài khoản với email: {WebUtility.HtmlEncode(user.Email)}</p>
                <p>Vui lòng bấm vào nút dưới đây để kích hoạt tài khoản:</p>
                <p>
                    <a href=""{confirmUrl}""
                       style=""background-color:#007bff;color:#fff;padding:10px 20px;
                              text-decoration:none;border-radius:5px;font-weight:bold;"">
                        XÁC NHẬN EMAIL
                    </a>
                </p>
                <p>Nếu nút trên không bấm được, hãy copy đường link sau dán vào trình duyệt:</p>
                <p>{confirmUrl}</p>";

            await _emailSender.SendAsync(user.Email!, subject, body);
        }

        // ================== XÁC NHẬN EMAIL ==================
        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return Content("Thiếu thông tin xác nhận.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Content("Không tìm thấy tài khoản.");
            }

            try
            {
                var decodedBytes = WebEncoders.Base64UrlDecode(token);
                var normalToken = Encoding.UTF8.GetString(decodedBytes);

                var result = await _userManager.ConfirmEmailAsync(user, normalToken);
                if (result.Succeeded)
                {
                    // Xác nhận thành công → quay về trang login
                    return Content(@"
                        <!DOCTYPE html>
                        <html>
                        <head><meta charset='utf-8'><title>Xác nhận thành công</title></head>
                        <body style='text-align:center; padding-top:50px; font-family:sans-serif;'>
                            <h1 style='color:green;'>Xác nhận email thành công!</h1>
                            <p>Tài khoản của bạn đã được kích hoạt.</p>
                            <a href='/html/login.html' style='background:#007bff;color:#fff;padding:10px 20px;text-decoration:none;border-radius:5px;'>Đăng nhập ngay</a>
                        </body>
                        </html>");
                }

                return Content("Xác nhận email thất bại.");
            }
            catch
            {
                return Content("Xác nhận email thất bại.");
            }
        }

        // ================== ĐĂNG NHẬP ==================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ." });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Sai email hoặc mật khẩu." });
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                return Unauthorized(new { message = "Sai email hoặc mật khẩu." });
            }

            // CHẶN LOGIN nếu chưa xác nhận email
            if (!user.EmailConfirmed)
            {
                return Unauthorized(new
                {
                    message = "Tài khoản chưa kích hoạt. Vui lòng kiểm tra email để xác nhận."
                });
            }

            var token = await GenerateJwtToken(user);

            return Ok(new
            {
                token,
                email = user.Email,
                fullName = user.FullName
            });
        }

        // ================== LẤY PROFILE ==================
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            return Ok(new
            {
                email = user.Email,
                userName = user.UserName,
                fullName = user.FullName,
                dateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd")
            });
        }

        // ================== CẬP NHẬT PROFILE ==================
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // 1. Cập nhật các trường thông tin thường
            user.FullName = model.FullName;
            user.DateOfBirth = model.DateOfBirth;

            // 2. Xử lý đổi UserName (nếu có gửi lên và khác tên cũ)
            if (!string.IsNullOrEmpty(model.UserName) && model.UserName != user.UserName)
            {
                // Kiểm tra xem tên mới có bị trùng không
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Tên người dùng này đã tồn tại. Vui lòng chọn tên khác." });
                }

                // Set cả UserName và NormalizedUserName
                await _userManager.SetUserNameAsync(user, model.UserName);
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { message = "Cập nhật hồ sơ thành công." });
            }

            return BadRequest(new { message = "Lỗi khi lưu thông tin." });
        }

        // 1. API Gửi yêu cầu quên mật khẩu
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            // Luôn trả về Ok để bảo mật, tránh bị dò email
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Vẫn báo thành công ảo để hacker không biết email này chưa tồn tại
                return Ok(new { message = "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu đã được gửi." });
            }

            // Tạo token reset password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Tạo link trỏ về trang HTML frontend
            // Lưu ý: Port mặc định đang là 5062. Nếu code chạy port khác, hãy sửa lại.
            var callbackUrl = $"{Request.Scheme}://{Request.Host}/html/reset-password.html?token={System.Net.WebUtility.UrlEncode(token)}&email={System.Net.WebUtility.UrlEncode(user.Email)}";

            var subject = "Đặt lại mật khẩu - BookingTourAPI";
            var body = $@"
                <div style='font-family:Arial,sans-serif; padding:20px; border:1px solid #ddd; border-radius:5px;'>
                    <h2 style='color:#007bff'>Yêu cầu đặt lại mật khẩu</h2>
                    <p>Chào {user.FullName},</p>
                    <p>Bạn vừa yêu cầu đặt lại mật khẩu. Vui lòng nhấn vào nút bên dưới để tiếp tục:</p>
                    <a href='{callbackUrl}' style='background-color:#007bff; color:white; padding:10px 20px; text-decoration:none; border-radius:5px; display:inline-block;'>Đặt lại mật khẩu</a>
                    <p style='margin-top:20px; color:#777; font-size:12px'>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
                </div>";

            await _emailSender.SendAsync(user.Email!, subject, body);

            return Ok(new { message = "Vui lòng kiểm tra email để đặt lại mật khẩu." });
        }

        // 2. API Thực hiện đổi mật khẩu (khi user bấm link trong mail)
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(new { message = "Yêu cầu không hợp lệ." });

            // --- BƯỚC THÊM MỚI: KIỂM TRA MẬT KHẨU CŨ ---
            // Kiểm tra xem mật khẩu mới có trùng với mật khẩu hiện tại không
            bool isSamePassword = await _userManager.CheckPasswordAsync(user, model.NewPassword);
            if (isSamePassword)
            {
                return BadRequest(new { message = "Mật khẩu mới không được trùng với mật khẩu cũ. Vui lòng chọn mật khẩu khác." });
            }
            // -------------------------------------------

            // Nếu không trùng, tiến hành đặt lại như bình thường
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            
            if (result.Succeeded)
            {
                return Ok(new { message = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập ngay." });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        // ================== TẠO JWT TOKEN ==================
        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                // Id người dùng
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),

                // Email
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Email ?? string.Empty),

                // Id token
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Thêm role (Admin/User)
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(30),   // Token sống 30 ngày
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // ================== DTO ==================
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Mật khẩu phải ít nhất 6 ký tự.")]
        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string? UserName { get; set; }
        [MaxLength(100)]
        public string? FullName { get; set; }

        public DateOnly? DateOfBirth { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
    }

    public class ResetPasswordRequest
    {
        [Required] public string Email { get; set; } = "";
        [Required] public string Token { get; set; } = "";
        [Required] public string NewPassword { get; set; } = "";
    }
}
