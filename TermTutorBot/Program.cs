using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TermTutorBot.Models;
using TermTutorBot.Services;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var settings = configuration
    .GetSection("BotSettings")
    .Get<BotSettings>();

if (settings == null)
{
    throw new Exception("BotSettings не завантажився. Перевір appsettings.json");
}

// ==========================================
// ЗАВАНТАЖЕННЯ ПРОМПТІВ
// ==========================================
string promptsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts");
string promptFilePath = Path.Combine(promptsFolder, "SystemPrompt.txt");

if (File.Exists(promptFilePath))
{
    settings.SystemPrompt = File.ReadAllText(promptFilePath);
}
else
{
    Console.WriteLine($"[Увага] Файл {promptFilePath} не знайдено. Буде використано промпт з appsettings.json.");
}

string LoadPrompt(string fileName, string defaultText)
{
    string fullPath = Path.Combine(promptsFolder, fileName);
    if (File.Exists(fullPath))
        return File.ReadAllText(fullPath);

    Console.WriteLine($"[Увага] Файл {fileName} не знайдено у папці Prompts. Використовую стандартний текст.");
    return defaultText;
}

string promptSenTemplate = LoadPrompt("PromptSentence.txt", "Склади 3 цікаві та зрозумілі приклади речень зі словом '{0}'.");
string promptSynTemplate = LoadPrompt("PromptSynonyms.txt", "Знайди синоніми до слова '{0}'. Якщо це предмет, напиши, що синонімів немає.");
string promptMorTemplate = LoadPrompt("PromptMoreInfo.txt", "Напиши 1-2 цікаві факти або коротку етимологію слова '{0}'.");

var botClient = new TelegramBotClient(settings.TelegramToken);
var httpClient = new HttpClient();
var openAiService = new OpenAIService(httpClient, settings);

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"Бот @{me.Username} запущений");
Console.ReadLine();
cts.Cancel();
async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{

}
Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    
}
