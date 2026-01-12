// TRONG FILE: Controllers/AdminUsersController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingTourAPI.Hubs;
using BookingTourAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.IO;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHubContext<AdminNotificationHub> _adminHubContext;
        private readonly IHubContext<UserNotificationHub> _userHubContext;

        // Sửa Constructor để inject UserNotificationHub
        public AdminUsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IHubContext<AdminNotificationHub> adminHubContext,
            IHubContext<UserNotificationHub> userHubContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _adminHubContext = adminHubContext;
            _userHubContext = userHubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new { user.Id, user.UserName, user.Email, user.LockoutEnd, Roles = roles });
            }
            return Ok(userList);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return Ok(roles);
        }

        [HttpPut("{userId}/roles")]
        public async Task<IActionResult> UpdateUserRoles(string userId, [FromBody] List<string> newRoles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(newRoles));
            if (!removeResult.Succeeded) return BadRequest(removeResult.Errors);
            var addResult = await _userManager.AddToRolesAsync(user, newRoles.Except(currentRoles));
            if (!addResult.Succeeded) return BadRequest(addResult.Errors);
            return Ok(new { message = "Cập nhật roles thành công." });
        }

        [HttpPost("{userId}/reset-password")]
        public async Task<IActionResult> ResetPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");
            var newPassword = Path.GetRandomFileName().Replace(".", "") + "A1!";
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded) return Ok(new { message = $"Đã reset mật khẩu thành công. Mật khẩu tạm thời: {newPassword}" });
            return BadRequest(result.Errors);
        }

        [HttpPut("{userId}/lock")]
        public async Task<IActionResult> LockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            if (result.Succeeded)
            {
                // Gửi cho các admin khác
                await _adminHubContext.Clients.All.SendAsync("UserLockStatusChanged", userId, true);
                // Gửi cho user bị ảnh hưởng
                await _userHubContext.Clients.Group(userId).SendAsync("AccountLocked", "Tài khoản của bạn đã bị quản trị viên khóa.");
                return Ok(new { message = "Đã khóa tài khoản." });
            }
            return BadRequest(result.Errors);
        }

        [HttpPut("{userId}/unlock")]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                // Gửi cho các admin khác
                await _adminHubContext.Clients.All.SendAsync("UserLockStatusChanged", userId, false);
                // Gửi cho user được mở khóa
                await _userHubContext.Clients.Group(userId).SendAsync("AccountUnlocked", "Tài khoản của bạn đã được mở khóa.");
                return Ok(new { message = "Đã mở khóa tài khoản." });
            }
            return BadRequest(result.Errors);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Không tìm thấy người dùng.");
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == userId)
            {
                return BadRequest(new { message = "Không thể xóa tài khoản admin đang đăng nhập." });
            }
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _adminHubContext.Clients.All.SendAsync("UserDeleted", userId);
                return Ok(new { message = "Đã xóa tài khoản." });
            }
            return BadRequest(result.Errors);
        }
    }
}