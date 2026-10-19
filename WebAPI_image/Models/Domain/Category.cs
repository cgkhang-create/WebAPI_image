namespace WebAPI_image.Models.Domain
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Quan hệ 1-n với Image
        public List<Image> Images { get; set; } = new();
    }
}