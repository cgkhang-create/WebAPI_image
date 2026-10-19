using System.ComponentModel.DataAnnotations;

namespace WebAPI_image.Models.DTO
{
    public class RegisterRequestDTO
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Chỉ chấp nhận "User" hoặc "Admin". Mặc định là "User".
        /// </summary>
        public string Role { get; set; } = "User";
    }
}