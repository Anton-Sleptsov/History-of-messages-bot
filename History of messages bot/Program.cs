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
            if (message != null)
            {
                var chatId = message.Chat.Id;
                string text = message.Text;

                if(message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                {
                    string userName = message.Chat.Username;
                    Console.WriteLine($"Из чата {chatId} пришло сообщение от \"{userName}\"" +
                        $" вот его текст \"{text}\"");
                }
                else
                {
                    string title = message.Chat.Title;
                    Console.WriteLine($"Из чата {chatId} с названием \"{title}\" пришло сообщение, " +
                        $" вот его текст \"{text}\"");
                }
            }
        }
    }
}