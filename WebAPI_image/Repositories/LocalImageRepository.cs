using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI_image.Data;
using WebAPI_image.Models.Domain;

namespace WebAPI_image.Repositories
{
    public class LocalImageRepository : IImageRepository
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _db;

        public LocalImageRepository(
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            AppDbContext db)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _db = db;
        }

        private string GetCurrentUsername()
        {
            return _httpContextAccessor.HttpContext?.User?
                       .FindFirstValue(ClaimTypes.Name)
                   ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name
                   ?? "Unknown";
        }

        private bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
        }

        public async Task<Image> UploadAsync(Image image)
        {
            var folder = Path.Combine(_env.ContentRootPath, "Images");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var uniqueFileName = $"{image.FileName}_{Guid.NewGuid():N}{image.FileExtension}";
            var localPath = Path.Combine(folder, uniqueFileName);

            using (var stream = new FileStream(localPath, FileMode.Create))
            {
                await image.File!.CopyToAsync(stream);
            }

            var request = _httpContextAccessor.HttpContext!.Request;

            // CategoryId đã được gán từ Controller (từ request.CategoryId)
            // Không cần gán lại ở đây

            image.FilePath = $"{request.Scheme}://{request.Host}{request.PathBase}/Images/{uniqueFileName}";
            image.FileName = Path.GetFileNameWithoutExtension(uniqueFileName);
            image.UploadedBy = GetCurrentUsername();

            _db.Images.Add(image);
            await _db.SaveChangesAsync();

            return image;
        }

        public async Task<List<Image>> GetAllAsync(int? categoryId = null)
        {
            var query = _db.Images
                .Include(i => i.Category)
                .AsQueryable();

            if (!IsAdmin())
            {
                var username = GetCurrentUsername();
                query = query.Where(x => x.UploadedBy == username);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == categoryId.Value);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<Image?> GetByIdAsync(int id)
        {
            var image = await _db.Images
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null) return null;

            // User thường không được xem ảnh của người khác
            if (!IsAdmin() && image.UploadedBy != GetCurrentUsername())
                return null;

            return image;
        }

        public async Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadAsync(int id)
        {
            var image = await GetByIdAsync(id);
            if (image == null) return null;

            var path = Path.Combine(_env.ContentRootPath, "Images", image.FileName + image.FileExtension);
            if (!File.Exists(path)) return null;

            var bytes = await File.ReadAllBytesAsync(path);
            return (bytes, "application/octet-stream", image.FileName + image.FileExtension);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var image = await _db.Images.FindAsync(id);
            if (image == null) return false;

            // Chỉ Admin hoặc chính chủ mới được xóa
            if (!IsAdmin() && image.UploadedBy != GetCurrentUsername())
                return false;

            var path = Path.Combine(_env.ContentRootPath, "Images", image.FileName + image.FileExtension);
            if (File.Exists(path))
                File.Delete(path);

            _db.Images.Remove(image);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}