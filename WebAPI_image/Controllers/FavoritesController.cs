using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI_image.Data;
using WebAPI_image.Models.Domain;

namespace WebAPI_image.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public FavoritesController(AppDbContext db)
        {
            _db = db;
        }

        private string GetCurrentUsername()
        {
            return User.FindFirstValue(ClaimTypes.Name)
                   ?? User.Identity?.Name
                   ?? "";
        }

        // GET: api/Favorites  → danh sách ảnh yêu thích của tôi
        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            var username = GetCurrentUsername();

            var favorites = await _db.Favorites
                .Include(f => f.Image)
                    .ThenInclude(i => i!.Category)
                .Where(f => f.Username == username)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    f.Id,
                    f.ImageId,
                    f.CreatedAt,
                    Image = f.Image == null ? null : new
                    {
                        f.Image.Id,
                        f.Image.FileName,
                        f.Image.FileDescription,
                        f.Image.FileExtension,
                        f.Image.FileSizeInBytes,
                        f.Image.FilePath,
                        f.Image.CreatedAt,
                        f.Image.UploadedBy,
                        f.Image.CategoryId,
                        CategoryName = f.Image.Category != null ? f.Image.Category.Name : null
                    }
                })
                .ToListAsync();

            return Ok(favorites);
        }

        // POST: api/Favorites/{imageId}  → thêm yêu thích
        [HttpPost("{imageId:int}")]
        public async Task<IActionResult> Add(int imageId)
        {
            var username = GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var image = await _db.Images.FindAsync(imageId);
            if (image == null)
                return NotFound("Không tìm thấy ảnh.");

            var exists = await _db.Favorites
                .AnyAsync(f => f.Username == username && f.ImageId == imageId);

            if (exists)
                return BadRequest("Ảnh này đã có trong danh sách yêu thích.");

            var favorite = new Favorite
            {
                Username = username,
                ImageId = imageId
            };

            _db.Favorites.Add(favorite);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Đã thêm vào yêu thích.", FavoriteId = favorite.Id });
        }

        // DELETE: api/Favorites/{imageId}  → bỏ yêu thích
        [HttpDelete("{imageId:int}")]
        public async Task<IActionResult> Remove(int imageId)
        {
            var username = GetCurrentUsername();

            var favorite = await _db.Favorites
                .FirstOrDefaultAsync(f => f.Username == username && f.ImageId == imageId);

            if (favorite == null)
                return NotFound("Ảnh không nằm trong danh sách yêu thích.");

            _db.Favorites.Remove(favorite);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Đã bỏ yêu thích." });
        }

        // GET: api/Favorites/check/{imageId}  → kiểm tra đã thích chưa
        [HttpGet("check/{imageId:int}")]
        public async Task<IActionResult> Check(int imageId)
        {
            var username = GetCurrentUsername();

            var isFavorite = await _db.Favorites
                .AnyAsync(f => f.Username == username && f.ImageId == imageId);

            return Ok(new { IsFavorite = isFavorite });
        }
    }
}