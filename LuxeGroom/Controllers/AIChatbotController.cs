/*
 * AIChatbotController.cs
 * Dedicated controller for the public AI chat widget on the LuxeGroom landing page.
 */

using Microsoft.AspNetCore.Mvc;
using LuxeGroom.Data;
using LuxeGroom.Data.Generated;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace LuxeGroom.Controllers
{
    [Route("AIChatbot")]
    public class AIChatbotController : Controller
    {
        private readonly LuxeGroomDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<AIChatbotController> _logger;

        public AIChatbotController(
            LuxeGroomDbContext context,
            IConfiguration config,
            IHttpClientFactory httpFactory,
            ILogger<AIChatbotController> logger)
        {
            _context = context;
            _config = config;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        [HttpGet("Context")]
        public IActionResult Context()
        {
            try
            {
                var record = _context.Chatbot
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefault();

                if (record == null)
                {
                    return Json(new
                    {
                        instructions = "No instructions configured yet.",
                        data = "No data configured yet."
                    });
                }

                return Json(new
                {
                    instructions = record.Instructions ?? "",
                    data = record.Data ?? ""
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Context endpoint failed");
                return Json(new
                {
                    instructions = $"Error loading instructions: {ex.Message}",
                    data = "Database temporarily unavailable"
                });
            }
        }

        [HttpPost("Ask")]
        public async Task<IActionResult> Ask([FromBody] AIChatRequest request)
        {
            if (request?.Messages == null || request.Messages.Count == 0)
                return Json(new { success = false, error = "No messages provided." });

            Chatbot record = null;

            try
            {
                record = _context.Chatbot
                    .OrderByDescending(c => c.CreatedDate)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Chatbot configuration");
                return Json(new { success = false, error = "Configuration unavailable." });
            }

            var systemPrompt = record == null
                ? "You are a helpful assistant for Luxe Groom, a premium pet grooming salon."
                : string.IsNullOrWhiteSpace(record.Data)
                    ? record.Instructions ?? ""
                    : $"{record.Instructions}\n\n--- Context / Data ---\n{record.Data}";

            var apiKey = _config["OpenAI:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("OpenAI API key missing.");
                return Json(new { success = false, error = "OpenAI not configured." });
            }

            try
            {
                var conversationText = new StringBuilder();
                conversationText.AppendLine(systemPrompt);

                foreach (var msg in request.Messages)
                {
                    conversationText.AppendLine($"{msg.Role}: {msg.Content}");
                }

                var payload = new
                {
                    model = "gpt-4o",
                    input = conversationText.ToString(),
                    max_output_tokens = 1024
                };

                var http = _httpFactory.CreateClient();
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var response = await http.PostAsync(
                    "https://api.openai.com/v1/responses",
                    new StringContent(
                        JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json"));

                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI error {Status}: {Body}", response.StatusCode, body);
                    return Json(new { success = false, error = "OpenAI request failed." });
                }

                using var doc = JsonDocument.Parse(body);

                var reply = doc.RootElement
                    .GetProperty("output")[0]
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                return Json(new
                {
                    success = true,
                    message = reply?.Trim() ?? ""
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected OpenAI error.");
                return Json(new { success = false, error = "Unexpected server error." });
            }
        }
    }

    public class AIChatRequest
    {
        public List<AIChatMessage> Messages { get; set; } = new();
    }

    public class AIChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}