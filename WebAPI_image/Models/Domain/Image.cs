using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace WebAPI_image.Models.Domain
{
    public class Image
    {
        public int Id { get; set; }

        [NotMapped]
        public IFormFile? File { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string? FileDescription { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public string? FilePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string UploadedBy { get; set; } = string.Empty;

        // ===== PHÂN LOẠI =====
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}