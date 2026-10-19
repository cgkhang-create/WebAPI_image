namespace WebAPI_image.Models.Domain
{
    public class Favorite
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;  // người đánh dấu
        public int ImageId { get; set; }

        public Image? Image { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}