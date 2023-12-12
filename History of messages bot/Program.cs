using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using MySql.Data.MySqlClient;

namespace History_of_messages_bot
{
    internal class Program
    {
        private static string _token { get; set; } = "6351710759:AAFcfAOI0pATZc3s4ggHdOUw3wnaRmSNKO0";
        private static TelegramBotClient _client;
        private static long _chatId = -1002129394383;

       private static MySqlConnection connection = new MySqlConnection("server=localhost;port=3306;username=root;password=;" +
           "database=History_of_messages");

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
            if (message.Text != null && message.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private && message.Chat.Id == _chatId)
            {
                var chatId = message.Chat.Id;
                string text = message.Text;
                string userName = message.From.Username;
                string title = message.Chat.Title;
                DateTime date = message.Date;

                Console.WriteLine($"{date} Из чата номер {chatId} с названием \"{title}\" пришло сообщение от пользователя {userName}, " +
                    $" вот его текст \"{text}\"");

                SaveMessageToDatabase(text, userName, date);
            }
            else if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, $"Добавьте бота в группу, чтобы он сохранял все сообщения из неё");
            }
            else if (message.Chat.Id != _chatId)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, $"К сожалению, данный бот создан только для служебного пользования, " +
                    $"но его исходный код можно посмотреть на гитхабе: https://github.com/Anton-Sleptsov/History-of-messages-bot.git " +
                    $"\nУдалите его из группы");
            }
        }

        private static void SaveMessageToDatabase(string text, string userName, DateTime date)
        {

            try
            {
                connection.Open();

                string query = "INSERT INTO History_in_group (Text, UserName, Date) VALUES (@Text, @UserName, @Date)";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Text", text);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@Date", date);

                    command.ExecuteNonQuery();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("При записи в базу что-то пошло нет так, а именно " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}