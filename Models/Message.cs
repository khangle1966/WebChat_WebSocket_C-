namespace WebChatServer.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string? UserName { get; set; } // Cho phép null
        public string? Text { get; set; } // Cho phép null
        public DateTime CreatedAt { get; set; } = DateTime.Now; // Đảm bảo rằng thời gian được khởi tạo
        public int ChatRoomId { get; set; }
    }
}
