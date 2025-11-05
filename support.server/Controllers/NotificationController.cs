using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Authentication;
using System.Text;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public NotificationController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] object payload)
        {
            var webhookUrl = _config["Teams:WebhookUrl"];
            if (string.IsNullOrEmpty(webhookUrl))
                return BadRequest("Webhook URL chưa được cấu hình.");

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(webhookUrl, content);

            if (response.IsSuccessStatusCode)
                return Ok("Đã gửi thông báo tới Teams.");

            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }
        //[HttpPost("send")]
        //public async Task<IActionResult> Send([FromBody] object payload)
        //{
        //    var webhookUrl = _config["Teams:WebhookUrl"];
        //    if (string.IsNullOrEmpty(webhookUrl))
        //        return BadRequest("Webhook URL chưa được cấu hình.");

        //    // Giới hạn timeout ngắn (10s) để không treo request lâu
        //    using var httpClient = new HttpClient
        //    {
        //        Timeout = TimeSpan.FromSeconds(10)
        //    };

        //    var jsonPayload = payload.ToString();
        //    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        //    const int maxRetry = 3;
        //    for (int attempt = 1; attempt <= maxRetry; attempt++)
        //    {
        //        try
        //        {
        //            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        //            var response = await httpClient.PostAsync(webhookUrl, content);
        //            stopwatch.Stop();

        //            if (response.IsSuccessStatusCode)
        //            {
        //                return Ok($"Đã gửi thông báo tới Teams. (Attempt {attempt}, {stopwatch.ElapsedMilliseconds} ms)");
        //            }

        //            var error = await response.Content.ReadAsStringAsync();
        //            //_logger.LogWarning("Gửi Teams thất bại lần {Attempt}: {StatusCode} - {Error}", attempt, response.StatusCode, error);

        //            // Nếu là lỗi tạm thời (5xx hoặc 429) thì retry
        //            if ((int)response.StatusCode >= 500 || (int)response.StatusCode == 429)
        //            {
        //                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 2s, 4s, 8s
        //                continue;
        //            }

        //            return StatusCode((int)response.StatusCode, error);
        //        }
        //        catch (TaskCanceledException ex)
        //        {
        //            //_logger.LogWarning("Timeout khi gửi Teams (Attempt {Attempt}): {Message}", attempt, ex.Message);
        //            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Retry với backoff
        //        }
        //        catch (HttpRequestException ex)
        //        {
        //            //_logger.LogWarning("Lỗi mạng khi gửi Teams (Attempt {Attempt}): {Message}", attempt, ex.Message);
        //            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        //        }
        //        catch (Exception ex)
        //        {
        //            //_logger.LogError(ex, "Lỗi không xác định khi gửi Teams (Attempt {Attempt})", attempt);
        //            return StatusCode(500, ex.Message);
        //        }
        //    }

        //    return StatusCode(504, "Không thể gửi thông báo tới Teams sau nhiều lần thử.");
        //}

    }
}
