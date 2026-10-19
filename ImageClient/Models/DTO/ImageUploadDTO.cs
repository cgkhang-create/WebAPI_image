using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ImageClient.Models.DTO
{
    public class ImageUploadDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn file ảnh")]
        public IFormFile? File { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên file")]
        public string FileName { get; set; } = string.Empty;

        public string? FileDescription { get; set; }

        // ===== THÊM =====
        public int? CategoryId { get; set; }
    }
}