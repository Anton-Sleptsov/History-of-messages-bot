using History_of_messages_web.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using Telegram.Bot;

namespace History_of_messages_web.Controllers
{
    public class AllMessagesFromDatabase : Controller
    {
        private static readonly MySqlConnection _connection = new MySqlConnection(System.IO.File.ReadAllText("..\\configs\\connectionDB.txt"));
        private static readonly string _tableName = System.IO.File.ReadAllText("..\\configs\\table.txt");
        List<Message> messages = new();

        public IActionResult Index()
        {
            try
            {
                _connection.Open();

                string query = $"SELECT * FROM {_tableName}";
                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                   FillList(messages, command);
                }
            }
            catch
            {
                return RedirectToAction("ConnectionError");
            }
            finally 
            { 
                _connection.Close(); 
            }
                
            return View(messages);
        }

        public IActionResult Relevant()
        {

            try
            {
                _connection.Open();

                string query = $"SELECT * FROM `{_tableName}` WHERE `MessageIsRelevant` = true ORDER BY `Date` DESC";
                using (MySqlCommand command = new MySqlCommand(query, _connection))
                {
                    FillList(messages, command);
                }
            }
            catch
            {
                return RedirectToAction("ConnectionError");
            }
            finally
            {
                _connection.Close();
            }

            return View(messages);
        }

        public IActionResult ConnectionError()
        {
            return View();
        }

        [HttpPost]
        public ActionResult MyButton_Click(int Id, string inputValue)
        {
            if(inputValue != null)
                SendMessage(Id, inputValue);

            return RedirectToAction("Relevant"); 
        }

        private async void SendMessage(int messageId, string text)
        {
            var botClient = new TelegramBotClient(System.IO.File.ReadAllText("..\\configs\\token.txt"));
            long chatId = long.Parse(System.IO.File.ReadAllText("..\\configs\\chat.txt"));

            try
            {
                await botClient.SendTextMessageAsync(chatId, text, replyToMessageId: messageId);
            }
            catch
            {
                return;
            }
            
        }

        private void FillList(List<Message> messages, MySqlCommand command)
        {
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32("Id");
                    int messageId = reader.GetInt32("MessageId");
                    string text = reader.GetString("Text");
                    string userName = reader.GetString("UserName");
                    DateTime date = reader.GetDateTime("Date");
                    bool messageIsRelevant = reader.GetBoolean("MessageIsRelevant");
                    int? originalId = reader.IsDBNull("OriginalId") ? (int?)null : reader.GetInt32("OriginalId");

                    messages.Add(new Message
                    {
                        Id = id,
                        MessageId = messageId,
                        Text = text,
                        UserName = userName,
                        Date = date,
                        MessageIsRelevant = messageIsRelevant,
                        OriginalId = originalId
                    });
                }
            }
        }
    }
}
