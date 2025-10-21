using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        // Dữ liệu mẫu lưu trong bộ nhớ
        private static readonly List<UserAccount> Users = new()
        {
            new UserAccount { Id = "user1", Email = "driver1@email.com", Phone = "0123456789", FullName = "Nguyen Van A", Password = "123456", Role = "EVDriver", CreatedAt = DateTime.UtcNow },
            new UserAccount { Id = "staff1", Email = "staff@email.com", Phone = "0987654321", FullName = "Tran Thi B", Password = "abcdef", Role = "Staff", CreatedAt = DateTime.UtcNow }
        };

        /// <summary>
        /// Lấy danh sách người dùng
        /// </summary>
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            return Ok(Users);
        }

        /// <summary>
        /// Lấy chi tiết một người dùng
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetUserById(string id)
        {
            var user = Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        [HttpPost]
        public IActionResult Register([FromBody] UserAccount account)
        {
            if (string.IsNullOrWhiteSpace(account.Email) || string.IsNullOrWhiteSpace(account.Password))
                return BadRequest("Email và Password là bắt buộc.");
            if (Users.Any(u => u.Email == account.Email))
                return Conflict("Email đã tồn tại.");
            account.Id = Guid.NewGuid().ToString();
            account.CreatedAt = DateTime.UtcNow;
            Users.Add(account);
            return CreatedAtAction(nameof(GetUserById), new { id = account.Id }, account);
        }

        /// <summary>
        /// Đăng nhập (giả lập, trả về thông tin nếu đúng email và password)
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] UserAccount login)
        {
            var user = Users.FirstOrDefault(u => u.Email == login.Email && u.Password == login.Password);
            if (user == null) return Unauthorized("Sai email hoặc mật khẩu.");
            return Ok(user);
        }
    }
}
