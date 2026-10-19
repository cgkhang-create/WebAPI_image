using WebAPI_image.Models.Domain;

namespace WebAPI_image.Repositories
{
    public interface IImageRepository
    {
        Task<Image> UploadAsync(Image image);
        Task<List<Image>> GetAllAsync(int? categoryId = null);
        Task<Image?> GetByIdAsync(int id);
        Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}