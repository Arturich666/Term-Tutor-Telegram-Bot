using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TermTutorBot.Models;

namespace TermTutorBot.Services;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly BotSettings _settings;

    public OpenAIService(HttpClient httpClient, BotSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.OpenAIApiKey);
    }

    public async Task<string> GetResponseAsync(string userMessage)
    {
        var requestBody = new
        {
            model = _settings.Model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = _settings.SystemPrompt
                },
                new
                {
                    role = "user",
                    content = userMessage
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/responses",
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        if (!response.IsSuccessStatusCode)
        {
            return $"Помилка OpenAI API: {response.StatusCode}";
        }

        var responseString = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseString);

        if (doc.RootElement.TryGetProperty("output", out var output))
        {
            foreach (var item in output.EnumerateArray())
            {
                if (item.TryGetProperty("content", out var content))
                {
                    foreach (var c in content.EnumerateArray())
                    {
                        if (c.TryGetProperty("text", out var text))
                        {
                            var result = text.GetString();
                            if (!string.IsNullOrEmpty(result))
                                return result;
                        }
                    }
                }
            }
        }

        return "Не вдалося отримати відповідь від моделі.";
    }
}