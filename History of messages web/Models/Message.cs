using System.ComponentModel.DataAnnotations.Schema;

namespace History_of_messages_web.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public string Text { get; set; }
        public string UserName { get; set; }
        public DateTime Date { get; set; }
        public bool MessageIsRelevant { get; set; }
        public int? OriginalId { get; set; }

        [ForeignKey("OriginalId")]
        public Message OriginalEntity { get; set; }
    }
}
