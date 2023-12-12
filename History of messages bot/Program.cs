using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using System.Data.SqlClient;

namespace History_of_messages_bot
{
    internal class Program
    {
        private static string _token { get; set; } = "6351710759:AAFcfAOI0pATZc3s4ggHdOUw3wnaRmSNKO0";
        private static TelegramBotClient _client;

        private static string connectionString = "your_connection_string";

        static void Main(string[] args)
        {
            _client = new TelegramBotClient(_token);
            _client.StartReceiving();
            _client.OnMessage += OnMessageHandler;
            Console.ReadLine();
            _client.StartReceiving();
        }

        private static async void OnMessageHandler(object? sender, MessageEventArgs e)
        {
            Message message = e.Message;
            if (message.Text != null && message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
            {
                var chatId = message.Chat.Id;
                string text = message.Text;
                string userName = message.From.Username;
                string title = message.Chat.Title;
                DateTime date = message.Date;

                Console.WriteLine($"{date} Из чата номер {chatId} с названием \"{title}\" пришло сообщение от пользователя {userName}, " +
                    $" вот его текст \"{text}\"");

                SaveMessageToDatabase(chatId, text, userName, date);
            }
            else if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, $"Добавьте бота в группу, чтобы он сохранял все сообщения из неё");
            }
        }

        private static void SaveMessageToDatabase(long chatId, string text, string userName, DateTime date)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO YourTableName (ChatId, Text, UserName, Date) VALUES (@ChatId, @Text, @UserName, @Date)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChatId", chatId);
                    command.Parameters.AddWithValue("@Text", text);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@Date", date);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}