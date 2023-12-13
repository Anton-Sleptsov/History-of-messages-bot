using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using MySql.Data.MySqlClient;
using Telegram.Bot.Types.ReplyMarkups;

namespace History_of_messages_bot
{
    internal class Program
    {
        private static readonly string _token = "6351710759:AAFcfAOI0pATZc3s4ggHdOUw3wnaRmSNKO0";
        private static TelegramBotClient _client;
        private static readonly long _chatId = -1002129394383;

        private static readonly MySqlConnection _connection = new MySqlConnection("server=localhost;port=3306;username=root;password=;" +
           "database=History_of_messages");

        private static string _testButton = "Узнать количество записей";

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
            if (message.Text != null && message.Chat.Id == _chatId)
            {
                await _client.SendTextMessageAsync(message.Chat.Id, $"Проверка работоспособности, нажмите \"{_testButton}\"", replyMarkup: GetButton());
                if (message.Text != _testButton)
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
                else 
                {
                    int count = GetNumberOfRecords();
                    await _client.SendTextMessageAsync(message.Chat.Id, $"Количество записей в таблице на данный момент - \"{count}\"");
                }
            }
            else if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
            {
                Console.WriteLine(message.Text);
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
                _connection.Open();

                string query = "INSERT INTO History_in_group (Text, UserName, Date) VALUES (@Text, @UserName, @Date)";

                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@Text", text);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@Date", date);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("При записи в базу что-то пошло нет так, а именно " + ex.Message);
            }
            finally
            {
                _connection.Close();
            }
      
        }

        private static int GetNumberOfRecords()
        {
            int count = 0;

            try
            {
                _connection.Open();

                string query = $"SELECT COUNT(*) FROM History_in_group";

                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    count = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            finally
            {
                _connection.Close();
            }             
            return count;
        }

        private static IReplyMarkup GetButton()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                { new List<KeyboardButton> {new KeyboardButton { Text = _testButton } } }
            };
        }
    }
}