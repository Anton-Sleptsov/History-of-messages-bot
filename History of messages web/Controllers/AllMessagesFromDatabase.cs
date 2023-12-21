using History_of_messages_web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace History_of_messages_web.Controllers
{
    public class AllMessagesFromDatabase : Controller
    {
        private static readonly MySqlConnection _connection = new MySqlConnection("server=localhost;port=3306;username=root;password=;database=History_of_messages");

        public IActionResult Index()
        {
            List<Message> messages = new();

            try
            {
                _connection.Open();

                string query = "SELECT * FROM History_in_group";
                using (MySqlCommand command = new MySqlCommand(query, _connection))
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
            finally 
            { 
                _connection.Close(); 
            }
                
            return View(messages);
        }
    }
}
