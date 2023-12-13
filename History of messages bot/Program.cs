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

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            _client = new TelegramBotClient(_token);

            _client.OnMessage += OnMessageHandler;
            _client.StartReceiving();

            // Запускаем поток для вывода количества записей каждый час
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    DisplayNumberOfRecords();
                    Thread.Sleep(1000 * 60 * 60);
                }
            }, TaskCreationOptions.LongRunning);

            Console.ReadLine();
        }

        private static async void DisplayNumberOfRecords()
        {
            try
            {
                int count = GetNumberOfRecords();
                await _client.SendTextMessageAsync(_chatId, $"Количество записей в базе на данный момент - \"{count}\"");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(DateTime.Now + " Выведено количество записей " + count);
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка вывода количества записей: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private static async void OnMessageHandler(object? sender, MessageEventArgs e)
        {
            Message message = e.Message;
            if (message.Text != null)
            {
                string chatIitle = message.Chat.Title ?? "Личные сообщения";
                string userName = message.From.Username ?? (message.From.FirstName + " " + message.From.LastName).Trim();
                Console.WriteLine($"{message.Date} Из чата номер {message.Chat.Id} с названием \"{chatIitle}\" пришло сообщение от пользователя {userName}, " +
                                  $" вот его текст \"{message.Text}\"");

                if (message.Chat.Id == _chatId)
                {
                    string text = message.Text;                   
                    DateTime date = message.Date;

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
            else if (message.Sticker != null)
            {
                string chatIitle = message.Chat.Title ?? "Личные сообщения";
                string userName = message.From.Username ?? (message.From.FirstName + " " + message.From.LastName).Trim();
                string text = $"Стикер \"{message.Sticker.Emoji}\"";
                Console.WriteLine($"{message.Date} Из чата номер {message.Chat.Id} с названием \"{chatIitle}\" пришло сообщение от пользователя {userName}, " +
                                  $" вот его текст \"{text}\"");

                if (message.Chat.Id == _chatId)
                {
                    DateTime date = message.Date;

                    SaveMessageToDatabase(text, userName, date);

                }
                else if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
                {
                    await _client.SendTextMessageAsync(message.Chat.Id, $"Добавьте бота в группу, чтобы он сохранял все сообщения из неё");
                }
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

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(DateTime.Now + " Запись добавлена в базу");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("При записи в базу что-то пошло нет так, а именно " + ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                _connection.Close();
            }
        }

        private static int GetNumberOfRecords()
        {
            int count = -1;

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
    }
}