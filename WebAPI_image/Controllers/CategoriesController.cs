using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI_image.Data;
using WebAPI_image.Models.Domain;
using WebAPI_image.Models.DTO;

namespace WebAPI_image.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/Categories
        [HttpGet]
        [AllowAnonymous]   // Cho phép xem danh sách loại mà không cần đăng nhập (nếu muốn)
        public async Task<IActionResult> GetAll()
        {
            var categories = await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                })
                .ToListAsync();

            return Ok(categories);
        }

        // GET: api/Categories/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return Ok(new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            });
        }

        // POST: api/Categories  (chỉ Admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Tên loại không được để trống.");

            var category = new Category
            {
                Name = request.Name.Trim(),
                Description = request.Description
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return Ok(new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            });
        }

        // DELETE: api/Categories/5  (chỉ Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}