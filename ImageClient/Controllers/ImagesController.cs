using ImageClient.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ImageClient.Controllers
{
    public class ImagesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl = "https://localhost:7066";

        public ImagesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateAuthorizedClient()
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        private bool IsLoggedIn()
        {
            return !string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("JWToken"));
        }

        private async Task<List<CategoryDTO>> GetCategoriesAsync()
        {
            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync($"{_apiBaseUrl}/api/Categories");

                if (!response.IsSuccessStatusCode)
                    return new List<CategoryDTO>();

                return await response.Content.ReadFromJsonAsync<List<CategoryDTO>>()
                       ?? new List<CategoryDTO>();
            }
            catch
            {
                return new List<CategoryDTO>();
            }
        }

        // GET: /Images
        public async Task<IActionResult> Index(int? categoryId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                using var client = CreateAuthorizedClient();

                var url = $"{_apiBaseUrl}/api/Images";
                if (categoryId.HasValue)
                    url += $"?categoryId={categoryId.Value}";

                var response = await client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Logout", "Auth");
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    ViewBag.Error = "Bạn không có quyền xem danh sách ảnh (403 Forbidden).";
                    ViewBag.Categories = await GetCategoriesAsync();
                    ViewBag.SelectedCategoryId = categoryId;
                    return View(new List<ImageDTO>());
                }

                response.EnsureSuccessStatusCode();

                var images = await response.Content
                    .ReadFromJsonAsync<List<ImageDTO>>() ?? new List<ImageDTO>();

                ViewBag.Categories = await GetCategoriesAsync();
                ViewBag.SelectedCategoryId = categoryId;

                return View(images);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Không thể tải danh sách ảnh: " + ex.Message;
                ViewBag.Categories = new List<CategoryDTO>();
                ViewBag.SelectedCategoryId = categoryId;
                return View(new List<ImageDTO>());
            }
        }

        // GET: /Images/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync($"{_apiBaseUrl}/api/Images/{id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (response.StatusCode == HttpStatusCode.NotFound || !response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không tìm thấy ảnh.";
                    return RedirectToAction(nameof(Index));
                }

                var image = await response.Content.ReadFromJsonAsync<ImageDTO>();
                if (image == null)
                {
                    TempData["Error"] = "Không tìm thấy ảnh.";
                    return RedirectToAction(nameof(Index));
                }

                return View(image);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi tải thông tin ảnh: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Images/Upload
        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            ViewBag.Categories = await GetCategoriesAsync();
            return View(new ImageUploadDTO());
        }

        // POST: /Images/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ImageUploadDTO model)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            ViewBag.Categories = await GetCategoriesAsync();

            if (model.File == null || model.File.Length == 0)
            {
                ModelState.AddModelError(nameof(model.File), "Vui lòng chọn file ảnh.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using var client = CreateAuthorizedClient();
                using var content = new MultipartFormDataContent();

                await using var stream = model.File!.OpenReadStream();
                using var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(model.File.ContentType)
                            ? "application/octet-stream"
                            : model.File.ContentType);

                content.Add(fileContent, "File", model.File.FileName);
                content.Add(new StringContent(model.FileName ?? string.Empty), "FileName");
                content.Add(new StringContent(model.FileDescription ?? string.Empty), "FileDescription");

                if (model.CategoryId.HasValue)
                {
                    content.Add(new StringContent(model.CategoryId.Value.ToString()), "CategoryId");
                }

                var response = await client.PostAsync($"{_apiBaseUrl}/api/Images/Upload", content);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    ViewBag.Error = "API trả về 401: JWT không hợp lệ hoặc đã hết hạn.";
                    return View(model);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadAsStringAsync();
                    ViewBag.Error = $"Upload thất bại: {(int)response.StatusCode}. {detail}";
                    return View(model);
                }

                TempData["Success"] = "Upload ảnh thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi upload ảnh: " + ex.Message;
                return View(model);
            }
        }

        // GET: /Images/Preview/5
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync($"{_apiBaseUrl}/api/Images/Download/{id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return Unauthorized();

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";

                return File(bytes, contentType);
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: /Images/Download/5
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync($"{_apiBaseUrl}/api/Images/Download/{id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không thể tải ảnh.";
                    return RedirectToAction(nameof(Index));
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = response.Content.Headers.ContentDisposition?
                    .FileName?.Trim('"') ?? $"image_{id}";

                return File(
                    bytes,
                    response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                    fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi tải ảnh: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Images/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.DeleteAsync($"{_apiBaseUrl}/api/Images/{id}");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    TempData["Error"] = "Bạn không có quyền xóa ảnh.";
                    return RedirectToAction(nameof(Index));
                }

                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Xóa ảnh thất bại.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = "Xóa ảnh thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa ảnh: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Images/ToggleFavorite/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                using var client = CreateAuthorizedClient();

                // Kiểm tra đã thích chưa
                var checkRes = await client.GetAsync($"{_apiBaseUrl}/api/Favorites/check/{id}");
                bool isFavorite = false;

                if (checkRes.IsSuccessStatusCode)
                {
                    var json = await checkRes.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("isFavorite", out var prop) ||
                        doc.RootElement.TryGetProperty("IsFavorite", out prop))
                    {
                        isFavorite = prop.GetBoolean();
                    }
                }

                HttpResponseMessage response;
                if (isFavorite)
                    response = await client.DeleteAsync($"{_apiBaseUrl}/api/Favorites/{id}");
                else
                    response = await client.PostAsync($"{_apiBaseUrl}/api/Favorites/{id}", null);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = isFavorite
                        ? "Đã bỏ khỏi yêu thích."
                        : "Đã thêm vào yêu thích.";
                }
                else
                {
                    var detail = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = string.IsNullOrWhiteSpace(detail)
                        ? "Không thể cập nhật yêu thích."
                        : detail;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
            }

            // Nếu đang ở trang Favorites thì quay lại đó
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && referer.Contains("Favorites", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(Favorites));

            return RedirectToAction(nameof(Index));
        }

        // GET: /Images/Favorites
        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Auth");

            try
            {
                using var client = CreateAuthorizedClient();
                var response = await client.GetAsync($"{_apiBaseUrl}/api/Favorites");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return RedirectToAction("Login", "Auth");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không tải được danh sách yêu thích.";
                    return View(new List<ImageDTO>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var list = new List<ImageDTO>();

                using var doc = JsonDocument.Parse(json);
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("image", out var img) ||
                        item.TryGetProperty("Image", out img))
                    {
                        var dto = JsonSerializer.Deserialize<ImageDTO>(
                            img.GetRawText(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (dto != null)
                            list.Add(dto);
                    }
                }

                return View(list);   // dùng Views/Images/Favorites.cshtml
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi: " + ex.Message;
                return View(new List<ImageDTO>());
            }
        }
    }
}