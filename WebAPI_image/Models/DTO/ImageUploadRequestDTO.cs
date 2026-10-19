using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebAPI_image.Models.DTO
{
    public class ImageUploadRequestDTO
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public string FileName { get; set; } = string.Empty;

        public string? FileDescription { get; set; }

        // ===== THÊM =====
        public int? CategoryId { get; set; }
    }
}