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
if (update.Type != UpdateType.Message || update.Message == null) 
        return;
        
    var message = update.Message;
    var currentChatId = message.Chat.Id;

    if (message.Type != MessageType.Text)
    {
        await bot.SendMessage(currentChatId, "Я працюю лише з текстовими повідомленнями.", cancellationToken: cancellationToken);
        return;
    }

    var text = message.Text?.Trim();
    if (string.IsNullOrWhiteSpace(text)) 
        return;

    if (text == "/start")
    {
        string welcomeText = 
            "Привіт! Я *Term Tutor* 🤖 - твій розумний помічник для вивчення нових слів та понять.\n\n" +
            "Ось що я вмію:\n" +
            "📖 *Пояснювати значення* - надішли мені будь-який термін чи слово, і я дам коротке та зрозуміле визначення.\n\n" +
            "Після кожного пояснення я запропоную тобі зручні кнопки:\n" +
            "📝 *Речення* - складу цікаві приклади використання слова.\n" +
            "🔄 *Синоніми* - підберу близькі за значенням слова.\n" +
            "💡 *Факти* - розповім коротку етимологію або цікавинку про слово.\n\n" +
            "Напиши мені перше слово, і почнемо! 👇";

        await bot.SendMessage(
            chatId: currentChatId,
            text: welcomeText,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
        return;
    }
    if (text.Length > 30 || text.Split(' ').Length > 3)
    {
        await bot.SendMessage(
            chatId: currentChatId, 
            text: "Будь ласка, надсилай мені **лише сам термін** (максимум 2-3 слова).\n\nЯкщо хочеш отримати приклад речення, синоніми чи факти - використовуй спеціальні кнопки, які з'являться після мого пояснення!", 
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
        return;
    }

    try
    {
        await bot.SendChatAction(currentChatId, ChatAction.Typing, cancellationToken: cancellationToken);
        var response = await openAiService.GetResponseAsync(text);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка: {ex.Message}");
        await bot.SendMessage(currentChatId, "Сталася помилка при обробці запиту.", cancellationToken: cancellationToken);
    }
}
Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    
}
