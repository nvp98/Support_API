using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.DTOs;
using support.server.Models;
using System.Net.Http;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _config = config;
        }
        [HttpPost("login")]
        public async Task<ActionResult<TicketLog>> Login(UserDto user)
        {
            try
            {
                // URL của authen service
                var authApiUrl = _config["authApiUrl:AuthAPI"];

                // Gửi request sang API xác thực
                var response = await _httpClient.PostAsJsonAsync(authApiUrl, user);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Xác thực thất bại",
                        status = response.StatusCode
                    });
                }

                // Đọc dữ liệu trả về (ví dụ token hoặc thông tin user)
                var result = await response.Content.ReadFromJsonAsync<object>();

                // Trả kết quả lại cho frontend
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi gọi API xác thực", detail = ex.Message });
            }
        }
    }
}
