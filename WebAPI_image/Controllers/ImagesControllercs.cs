using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI_image.Models.Domain;
using WebAPI_image.Models.DTO;
using WebAPI_image.Repositories;

namespace WebAPI_image.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly IImageRepository _repo;

        public ImagesController(IImageRepository repo)
        {
            _repo = repo;
        }

        // POST: api/Images/Upload
        [HttpPost("Upload")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Upload([FromForm] ImageUploadRequestDTO request)
        {
            ValidateFile(request);
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var image = new Image
            {
                File = request.File,
                FileName = request.FileName,
                FileExtension = Path.GetExtension(request.File.FileName).ToLower(),
                FileSizeInBytes = request.File.Length,
                FileDescription = request.FileDescription,
                CategoryId = request.CategoryId
            };

            var result = await _repo.UploadAsync(image);

            // Trả về object đơn giản, tránh lỗi vòng lặp JSON
            return Ok(new
            {
                result.Id,
                result.FileName,
                result.FileDescription,
                result.FileExtension,
                result.FileSizeInBytes,
                result.FilePath,
                result.CreatedAt,
                result.UploadedBy,
                result.CategoryId
            });
        }

        // GET: api/Images
        [HttpGet]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetAll([FromQuery] int? categoryId = null)
        {
            var images = await _repo.GetAllAsync(categoryId);

            var result = images.Select(i => new
            {
                i.Id,
                i.FileName,
                i.FileDescription,
                i.FileExtension,
                i.FileSizeInBytes,
                i.FilePath,
                i.CreatedAt,
                i.UploadedBy,
                i.CategoryId,
                CategoryName = i.Category != null ? i.Category.Name : null
            });

            return Ok(result);
        }

        // GET: api/Images/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var image = await _repo.GetByIdAsync(id);
            if (image == null) return NotFound();

            return Ok(new
            {
                image.Id,
                image.FileName,
                image.FileDescription,
                image.FileExtension,
                image.FileSizeInBytes,
                image.FilePath,
                image.CreatedAt,
                image.UploadedBy,
                image.CategoryId,
                CategoryName = image.Category != null ? image.Category.Name : null
            });
        }

        // GET: api/Images/Download/{id}
        [HttpGet("Download/{id:int}")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Download(int id)
        {
            var result = await _repo.DownloadAsync(id);
            if (result == null) return NotFound();

            return File(result.Value.FileBytes, result.Value.ContentType, result.Value.FileName);
        }

        // DELETE: api/Images/{id}  (chỉ Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _repo.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        private void ValidateFile(ImageUploadRequestDTO request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a file.");
                return;
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(request.File.FileName).ToLower();

            if (!allowed.Contains(ext))
                ModelState.AddModelError("File", "Only .jpg, .jpeg, .png, .gif, .webp are allowed.");

            if (request.File.Length > 10 * 1024 * 1024)
                ModelState.AddModelError("File", "File size must be less than 10MB.");
        }
    }
}