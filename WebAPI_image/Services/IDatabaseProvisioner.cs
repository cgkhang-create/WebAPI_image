namespace WebAPI_image.Services
{
    public interface IDatabaseProvisioner
    {
        /// <summary>
        /// Tạo database riêng cho user và bảng Images.
        /// Trả về tên database đã tạo.
        /// </summary>
        Task<string> CreateUserDatabaseAsync(string username);
    }
}