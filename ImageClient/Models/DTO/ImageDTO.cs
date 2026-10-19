namespace ImageClient.Models.DTO
{
    public class ImageDTO
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FileDescription { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public string? FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UploadedBy { get; set; } = string.Empty;

        // ===== PHÂN LOẠI =====
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}