using History_of_messages_web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using Telegram.Bot;

namespace History_of_messages_web.Controllers
{
    public class AllMessagesFromDatabase : Controller
    {
        private static readonly MySqlConnection _connection = new MySqlConnection("server=localhost;port=3306;username=root;password=;database=History_of_messages");
        private static readonly string _tableName = "History_in_group";
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
            finally
            {
                _connection.Close();
            }

            return View(messages);
        }

        [HttpPost]
        public ActionResult MyButton_Click(int Id, string inputValue)
        {
            SendMessage(Id, inputValue);

            return RedirectToAction("Relevant"); 
        }

        private async void SendMessage(int messageId, string text)
        {
            var botClient = new TelegramBotClient("6351710759:AAFcfAOI0pATZc3s4ggHdOUw3wnaRmSNKO0");
            long chatId = -1002004508817;

            await botClient.SendTextMessageAsync(chatId, text, replyToMessageId: messageId);
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
