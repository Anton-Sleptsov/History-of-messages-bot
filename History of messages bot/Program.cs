using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using MySql.Data.MySqlClient;
using File = System.IO.File;

namespace History_of_messages_bot
{
    internal class Program
    {
        private static readonly string _token = File.ReadAllText("..\\..\\..\\..\\configs\\token.txt"); //Бот 
        private static TelegramBotClient _client;

        private static readonly long _chatId = long.Parse(File.ReadAllText("..\\..\\..\\..\\configs\\chat.txt")); //Чат, в котором бот будет работать

        private static readonly string _connectionString = File.ReadAllText("..\\..\\..\\..\\configs\\connectionDB.txt");
        private static readonly MySqlConnection _connection = new MySqlConnection(_connectionString); //База, где будут храниться сообщения
        private static readonly string _databaseName = GetDatabaseName(_connectionString);
        private static readonly string _tableName = File.ReadAllText("..\\..\\..\\..\\configs\\table.txt");

        private static string GetDatabaseName(string connectionString)
        {
            string databaseName = "";
            int lastIndex = connectionString.LastIndexOf("database=");
            if (lastIndex != -1)
            {
                databaseName = connectionString.Substring(lastIndex + "database=".Length).Trim(';');
            }
            return databaseName;
        }

        private enum Columns
        {
            Id,
            MessageId,
            Text,
            UserName,
            Date,
            MessageIsRelevant,
            OriginalId
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            _client = new TelegramBotClient(_token);

            _client.OnMessage += OnMessageHandler;
            _client.OnMessageEdited += OnMessageEditedHandler;
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
                if (!TableExists())
                {
                    CreateTable();
                }

                int count = GetNumberOfRecords();
                await _client.SendTextMessageAsync(_chatId, $"Количество записей в базе на данный момент - \"{count}\"");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(DateTime.Now + " Выведено количество записей " + count);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка вывода количества записей: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static async void OnMessageHandler(object? sender, MessageEventArgs e)
        {
            Message message = e.Message;
            if (message.Chat.Id == _chatId)
            {
                ProcessMessageFromChat(message, messageEdited: false);
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

        private static void OnMessageEditedHandler(object? sender, MessageEventArgs e)
        {
            Message message = e.Message;
            if (message.Chat.Id == _chatId)
            {
                int oroginalId = GetOriginalId(message.MessageId);
                if (oroginalId == 0)
                {
                    ProcessMessageFromChat(message, messageEdited: false);
                }
                else
                {
                    MarkOriginalMessage(oroginalId);
                    ProcessMessageFromChat(message, messageEdited: true, oroginalId);
                }
            }
        }

        private static int GetOriginalId(int messageId)
        {
            int originalId = 0;

            try
            {
                _connection.Open();

                string query = $"SELECT Id FROM {_tableName} WHERE {Columns.MessageId} = @MessageId ORDER BY Id DESC LIMIT 1";

                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@MessageId", messageId);
                    originalId = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            catch (Exception ex) 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Не удалось получить id оригинального сообщения по причине " + ex.Message);
                Console.ResetColor();
            }
            finally
            {
                _connection.Close();
            }
            return originalId;
        }

        private static void ProcessMessageFromChat(Message message, bool messageEdited, int? originalId = null, bool MessageIsRelevant = true)
        {
            int messageId = message.MessageId;
            DateTime date = message.Date;
            string groupTitle = message.Chat.Title;
            string userName = message.From.Username ?? (message.From.FirstName + " " + message.From.LastName).Trim();

            if (message.Text != null)
            {
                string text = message.Text;

                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
            else if (message.Sticker != null)
            {
                string text = $"Стикер \"{message.Sticker.Emoji}\"";

                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
            else if (message.Photo != null)
            {
                string text = message.Caption != null ? $"Фоторафия с текстом \"{message.Caption}\"" : "Фотография";

                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
            else if (message.Animation != null)
            {
                string gif = message.Animation.FileName ?? message.Animation.FileId;
                string text = $"Гиф-изображение \"{gif}\"";

                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
            else if (message.Document != null)
            {
                string document = message.Document.FileName ?? message.Document.FileId;
                string text = message.Caption != null ? $"Документ \"{document}\" с текстом \"{message.Caption}\"" : $"Документ \"{document}\"";

                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
            else if (message.Audio != null)
            {
                string audio = message.Audio.FileName ?? message.Audio.FileId;
                string text = message.Caption != null ? $"Аудио-файл \"{audio}\" с текстом \"{message.Caption}\"" : $"Аудио-файл \"{audio}\"";

                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
            else if (message.Voice != null)
            {
                string text = $"Голосовое сообщение длительностью: {message.Voice.Duration} сек.";

                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
            else if (message.VideoNote != null)
            {
                string text = $"Кружочек длительностью: {message.VideoNote.Duration} сек.";

                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
            else
            {
                string text = $"Сообщение с неизвестным типом, а именно \"{message.Type}\"";

                Console.ForegroundColor = ConsoleColor.Red;
                MakeLoggingIncomingMessages(date, groupTitle, userName, text, messageEdited);
                Console.WriteLine("Неизвестный тип " + message.Type + "!!!");
                Console.ResetColor();
                SaveMessageToDatabase(messageId, text, userName, date, MessageIsRelevant, originalId);
            }
        }

        private static void MarkOriginalMessage(int originalId)
        {
            try
            {
                _connection.Open();

                string query = $"UPDATE {_tableName} SET {Columns.MessageIsRelevant} = false WHERE {Columns.Id} = @OriginalId";

                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@OriginalId", originalId);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Не удалось отметить оригинальное сообщение неактуальным по причине " + ex.Message);
                Console.ResetColor();
            }
            finally
            {
                _connection.Close();
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

        private static void MakeLoggingIncomingMessages(DateTime date, string chatIitle, string userName, string text, bool messageEdited)
        {
            if (!messageEdited)
                Console.WriteLine($"{date} Из рабочего чата с названием \"{chatIitle}\" пришло сообщение от пользователя {userName}, " +
                     $" вот его текст \"{text}\"");
            else
                Console.WriteLine($"{date} В рабочем чате с названием \"{chatIitle}\" было отредактировано сообщение от пользователя {userName}, " +
                     $" вот его новый текст \"{text}\"");
        }

        private static bool TableExists()
        {
            try
            {
                _connection.Open();

                using (MySqlCommand command = new MySqlCommand($"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{_databaseName}' AND table_name = '{_tableName}'", _connection))
                {
                    if (Convert.ToInt32(command.ExecuteScalar()) == 1)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Не удалось проверить, существует ли таблица по причине " + ex.Message);
                Console.ResetColor();
                return false;
            }
            finally
            {
                _connection.Close();
            }
        }

        private static void CreateTable()
        {
            try
            {
                _connection.Open();

                string query = $@"CREATE TABLE IF NOT EXISTS `{_tableName}` (
                                    `{Columns.Id}` INT(11) AUTO_INCREMENT PRIMARY KEY,
                                    `{Columns.MessageId}` INT(11) NOT NULL,
                                    `{Columns.Text}` TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                                    `{Columns.UserName}` VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
                                    `{Columns.Date}` DATETIME NOT NULL,
                                    `{Columns.MessageIsRelevant}` BOOLEAN NOT NULL,
                                    `{Columns.OriginalId}` INT(11) NULL,
                                    FOREIGN KEY (`{Columns.OriginalId}`) REFERENCES `{_tableName}`(`{Columns.Id}`)
                                ) DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    command.ExecuteNonQuery();
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Таблица {_tableName} успешно создана.");
                Console.ResetColor();

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка при создании таблицы: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                _connection.Close();
            }
        }

        private static void SaveMessageToDatabase(int messageId, string text, string userName, DateTime date, bool messageIsRelevant, int? originalId = null)
        {
            if (!TableExists())
            {
                CreateTable();
            }

            try
            {
                _connection.Open();

                string query = $"INSERT INTO {_tableName} ({Columns.MessageId}, {Columns.Text}, {Columns.UserName}, " +
                    $"{Columns.Date}, {Columns.MessageIsRelevant}, {Columns.OriginalId}) " +
                    "VALUES (@MessageId, @Text, @UserName, @Date, @MessageIsRelevant, @OriginalId)";

                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@MessageId", messageId);
                    command.Parameters.AddWithValue("@Text", text);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@Date", date);
                    command.Parameters.AddWithValue("@MessageIsRelevant", messageIsRelevant);
                    command.Parameters.AddWithValue("@OriginalId", originalId);

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

                string query = $"SELECT COUNT(*) FROM {_tableName}";

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