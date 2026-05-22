namespace TermTutorBot.Models;

public class BotSettings
{
    public string TelegramToken { get; set; } = "";
    public string OpenAIApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
}