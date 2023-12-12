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
            Console.ReadLine();
            _client.StartReceiving();
        }
    }
}