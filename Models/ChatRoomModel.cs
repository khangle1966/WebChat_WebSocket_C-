namespace WebChatServer.Models
{
    public class ChatRoomModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime LastActive { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastMessageTimestamp { get; set; } // Thêm thuộc tính này
    }
}
