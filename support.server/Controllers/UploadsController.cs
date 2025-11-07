using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadsController : ControllerBase
    {
        [HttpPost]
        [Route("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }
            // Thư mục lưu file: wwwroot/uploads
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);
            

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Đặt tên file theo timestamp + extension
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về URL để TinyMCE chèn ảnh
            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";

            return Ok(new { location = fileUrl });
        }
    }
}
