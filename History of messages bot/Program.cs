using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace History_of_messages_bot
{
    internal class Program
    {
        private static string _token { get; set; } = "6351710759:AAFcfAOI0pATZc3s4ggHdOUw3wnaRmSNKO0";
        private static TelegramBotClient _client;

        static void Main(string[] args)
        {
            _client = new TelegramBotClient(_token);
            _client.StartReceiving();
            _client.OnMessage += OnMessageHandler;
            Console.ReadLine();
            _client.StartReceiving();
        }

        private static void OnMessageHandler(object? sender, MessageEventArgs e)
        {
            Message message = e.Message;
            if (message != null && message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
            {
                var chatId = message.Chat.Id;
                string text = message.Text;
                string userName = message.From.Username;
                string title = message.Chat.Title;
                DateTime date = message.Date;

                Console.WriteLine($"{date} Из чата номер {chatId} с названием \"{title}\" пришло сообщение от пользователя {userName}, " +
                    $" вот его текст \"{text}\"");
            }
        }
    }
}