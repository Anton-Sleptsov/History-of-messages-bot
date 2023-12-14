using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using MySql.Data.MySqlClient;
using Telegram.Bot.Types.ReplyMarkups;

namespace History_of_messages_bot
{
    internal class Program
    {
        private static readonly string _token = "6351710759:AAFcfAOI0pATZc3s4ggHdOUw3wnaRmSNKO0"; //Бот @HistoryOfMessages_bot
        private static TelegramBotClient _client;
        private static readonly long _chatId = -1002129394383; //Чат, в котором бот будет работать

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
                    Thread.Sleep(1000 * 60 * 5);
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
            if (message.Chat.Id == _chatId)
            {
                DateTime date = message.Date;
                string groupTitle = message.Chat.Title;
                string userName = message.From.Username ?? (message.From.FirstName + " " + message.From.LastName).Trim();

                if (message.Text != null)
                {
                    string text = message.Text;

                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    SaveMessageToDatabase(text, userName, date);
                }
                else if (message.Sticker != null)
                {
                    string text = $"Стикер \"{message.Sticker.Emoji}\"";

                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    SaveMessageToDatabase(text, userName, date);
                }
                else if (message.Photo != null)
                { 
                    string text = message.Caption != null ? $"Фоторафия с текстом \"{message.Caption}\"" : "Фотография";

                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    SaveMessageToDatabase(text, userName, date);
                }
                else if (message.Document != null)
                {
                    string document = message.Document.FileName ?? message.Document.FileId;
                    string text = $"Документ \"{document}\"";

                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    SaveMessageToDatabase(text, userName, date);
                }
                else if (message.Animation != null)
                {
                    string gif = message.Animation.FileName ?? message.Animation.FileId;
                    string text = $"Гиф-изображение \"{gif}\"";

                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    SaveMessageToDatabase(text, userName, date);
                }
                else if (message.Audio != null)
                {
                    string audio = message.Audio.FileName ?? message.Audio.FileId;
                    string text = $"Аудио-файл \"{audio}\"";

                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    SaveMessageToDatabase(text, userName, date);
                }
                else if (message.Voice != null)
                {
                    string text = $"Голосовое сообщение длительностью: {message.Voice.Duration} сек.";

                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    SaveMessageToDatabase(text, userName, date);
                }
                else if (message.VideoNote != null)
                {
                    string text = $"Кружочек длительностью: {message.VideoNote.Duration} сек.";

                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    SaveMessageToDatabase(text, userName, date);
                }
                else
                {
                    string text = $"Сообщение с неизвестным типом, а именно \"{message.Type}\"";

                    Console.ForegroundColor = ConsoleColor.Red;
                    MakeLoggingIncomingMessages(date, groupTitle, userName, text);
                    Console.WriteLine("Неизвестный тип " + message.Type + "!!!");
                    Console.ResetColor();
                    SaveMessageToDatabase(text, userName, date);
                }
            }
            else if (message.Chat.Type == Telegram.Bot.Types.Enums.ChatType.Private)
            {
                DateTime date = message.Date;
                long chatId = message.Chat.Id;
                string chatTitle = "Личные сообщения";
                string userName = message.From.Username ?? (message.From.FirstName + " " + message.From.LastName).Trim();

                await _client.SendTextMessageAsync(message.Chat.Id, $"Добавьте бота в группу, чтобы он сохранял все сообщения из неё");
                MakeLoggingIncomingMessages(date, chatId, chatTitle, userName);
            }
            else
            {
                DateTime date = message.Date;
                long chatId = message.Chat.Id;
                string chatTitle = message.Chat.Title;
                string userName = message.From.Username ?? (message.From.FirstName + " " + message.From.LastName).Trim();

                await _client.SendTextMessageAsync(message.Chat.Id, $"К сожалению, данный бот создан только для служебного пользования, " +
                $"но его исходный код можно посмотреть на гитхабе: https://github.com/Anton-Sleptsov/History-of-messages-bot.git " +
                $"\nУдалите его из группы");
                MakeLoggingIncomingMessages(date, chatId, chatTitle, userName);
            }
        }

        private static void MakeLoggingIncomingMessages(DateTime date, long chatId, string chatIitle, string userName)
        {
            if (chatIitle == "Личные сообщения")
                Console.WriteLine($"{date} Из чата номер {chatId} с названием \"{chatIitle}\" пришло сообщение от пользователя {userName}," +
                                  $" была отправлена просьба добавить бота в чат");
            else
                Console.WriteLine($"{date} Из чата номер {chatId} с названием \"{chatIitle}\" пришло сообщение от пользователя {userName}," +
                                  $" была отправлена ссылка на гитхаб");
        }

        private static void MakeLoggingIncomingMessages(DateTime date, string chatIitle, string userName, string text)
        {
            Console.WriteLine($"{date} Из рабочего чата с названием \"{chatIitle}\" пришло сообщение от пользователя {userName}, " +
                  $" вот его текст \"{text}\"");
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
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("При записи в базу что-то пошло нет так, а именно " + ex.Message);
                Console.ResetColor();
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