
using ImageClient.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace ImageClient.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiBaseUrl = "https://localhost:7066";

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(
                HttpContext.Session.GetString("JWToken")))
            {
                return RedirectToAction("Index", "Images");
            }

            ViewBag.Success = TempData["RegisterSuccess"];
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập tài khoản và mật khẩu.";
                return View();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.PostAsJsonAsync(
                    $"{ApiBaseUrl}/api/Auth/Login",
                    new
                    {
                        Username = username.Trim(),
                        Password = password
                    });

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error =
                        "Đăng nhập thất bại. Hãy kiểm tra tài khoản và mật khẩu.";
                    return View();
                }

                var result = await response.Content
                    .ReadFromJsonAsync<LoginResponseDTO>();

                if (result == null ||
                    string.IsNullOrWhiteSpace(result.JwtToken))
                {
                    ViewBag.Error = "API không trả về JWT token hợp lệ.";
                    return View();
                }

                HttpContext.Session.Remove("JWToken");
                HttpContext.Session.SetString("JWToken", result.JwtToken);

                return RedirectToAction("Index", "Images");
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Không kết nối được API tại https://localhost:7066.";
                return View();
            }
            catch (JsonException)
            {
                ViewBag.Error = "Dữ liệu phản hồi từ API không hợp lệ.";
                return View();
            }
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string username,
            string password,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                var response = await client.PostAsJsonAsync(
                    $"{ApiBaseUrl}/api/Auth/Register",
                    new
                    {
                        Username = username.Trim(),
                        Password = password
                    });

                if (response.IsSuccessStatusCode)
                {
                    TempData["RegisterSuccess"] =
                        "Đăng ký thành công! Hãy đăng nhập.";

                    return RedirectToAction(nameof(Login));
                }

                var error = await response.Content.ReadAsStringAsync();

                ViewBag.Error = $"Đăng ký thất bại: {error}";
                return View();
            }
            catch (HttpRequestException)
            {
                ViewBag.Error =
                    "Không kết nối được API tại https://localhost:7066.";
                return View();
            }
            catch (JsonException)
            {
                ViewBag.Error = "Không đọc được phản hồi từ API.";
                return View();
            }
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JWToken");
            return RedirectToAction(nameof(Login));
        }
    }
}
