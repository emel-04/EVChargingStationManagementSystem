using EVChargingStation.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using EVChargingStation.Web.Models;
using EVChargingStation.Shared.Models;

namespace EVChargingStation.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiService _apiService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApiService apiService, ILogger<HomeController> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var jsonData = await _apiService.GetAsync<object>("api/station");
            var stations = JsonSerializer.Deserialize<List<StationDto>>(
                jsonData?.ToString() ?? "[]",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            ViewBag.Stations = stations ?? new List<StationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading stations");
            ViewBag.Stations = new List<StationDto>();
        }
        return View();
    }

    public IActionResult Login() => View();
    public IActionResult Register() => View();
    public async Task<IActionResult> Dashboard()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login");

        try
        {
            // Lấy tất cả bookings của user
            var bookings = await _apiService.GetAsync<List<BookingDto>>($"api/booking/user/{userId}")
                           ?? new List<BookingDto>();

            // Lấy tất cả payments của user
            var payments = await _apiService.GetAsync<List<PaymentDto>>($"api/payment/user/{userId}")
                           ?? new List<PaymentDto>();

            // Lấy danh sách stations để hiển thị tên
            var stations = await _apiService.GetAsync<List<StationDto>>("api/station")
                           ?? new List<StationDto>();

            // Tính toán thống kê
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.CompletedBookings = bookings.Count(b => b.Status == 3); // Status 3 = Completed
            ViewBag.TotalSpent = payments.Where(p => p.Status == 1).Sum(p => p.Amount); // Status 1 = Completed payment

            // Lấy 5 booking gần nhất
            var recentBookings = bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToList();

            // Ghép tên station vào booking
            foreach (var booking in recentBookings)
            {
                var station = stations.FirstOrDefault(s => s.Id == booking.StationId);
                booking.StationName = station?.Name ?? $"Trạm #{booking.StationId}";
            }

            ViewBag.RecentBookings = recentBookings;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            ViewBag.TotalBookings = 0;
            ViewBag.CompletedBookings = 0;
            ViewBag.TotalSpent = 0;
            ViewBag.RecentBookings = new List<BookingDto>();
            return View();
        }
    }
    public IActionResult Reports() => View();

    public async Task<IActionResult> Stations()
    {
        try
        {
            var jsonData = await _apiService.GetAsync<object>("api/station");
            var stations = JsonSerializer.Deserialize<List<StationDto>>(
                jsonData?.ToString() ?? "[]",
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            ViewBag.Stations = stations ?? new List<StationDto>();
        }
        catch
        {
            ViewBag.Stations = new List<StationDto>();
        }
        return View();
    }






    // ✅ LOGIN FIXED
    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        try
        {
            var loginData = new { email, password };
            var result = await _apiService.PostAsync<object>("api/auth/login", loginData);

            if (result is JsonElement json)
            {
                if (json.TryGetProperty("token", out var tokenProp))
                {
                    var token = tokenProp.GetString() ?? "";
                    var userId = json.GetProperty("user").GetProperty("id").GetInt32().ToString();
                    var firstName = json.GetProperty("user").GetProperty("firstName").GetString() ?? "";

                    HttpContext.Session.SetString("Token", token);
                    HttpContext.Session.SetString("UserId", userId);
                    HttpContext.Session.SetString("UserName", firstName);

                    return RedirectToAction("Dashboard");
                }

                if (json.TryGetProperty("message", out var msg))
                    ViewBag.Error = msg.GetString() ?? "Đăng nhập thất bại.";
                else
                    ViewBag.Error = "Đăng nhập thất bại. Dữ liệu không hợp lệ.";
            }
            else
            {
                ViewBag.Error = "Không nhận được phản hồi hợp lệ từ máy chủ.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            ViewBag.Error = "Đăng nhập thất bại. Lỗi hệ thống.";
        }

        return View();
    }

    // ✅ REGISTER FIXED
    [HttpPost]
    public async Task<IActionResult> Register(string firstName, string lastName, string email, string password, string phoneNumber)
    {
        try
        {
            // Validate và trim các trường
            firstName = firstName?.Trim() ?? string.Empty;
            lastName = lastName?.Trim() ?? string.Empty;
            email = email?.Trim() ?? string.Empty;
            password = password ?? string.Empty;
            phoneNumber = phoneNumber?.Trim() ?? string.Empty;

            // Validate các trường bắt buộc
            if (string.IsNullOrWhiteSpace(firstName))
            {
                ViewBag.Error = "Vui lòng nhập Họ.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                ViewBag.Error = "Vui lòng nhập Tên.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Vui lòng nhập Email.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập Mật khẩu.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ViewBag.Error = "Vui lòng nhập Số điện thoại.";
                return View();
            }

            var registerData = new
            {
                firstName = firstName,
                lastName = lastName,
                email = email,
                password = password,
                phoneNumber = phoneNumber,
                role = 1 // EVDriver
            };

            _logger.LogInformation("📤 Register data: FirstName={FirstName}, LastName={LastName}, Email={Email}, PhoneNumber={PhoneNumber}", 
                firstName, lastName, email, phoneNumber);

            var result = await _apiService.PostAsync<object>("api/auth/register", registerData);

            if (result is JsonElement json)
            {
                if (json.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    ViewBag.Success = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return View("Login");
                }

                if (json.TryGetProperty("message", out var msg))
                    ViewBag.Error = msg.GetString() ?? "Đăng ký thất bại.";
                else
                    ViewBag.Error = "Đăng ký thất bại. Vui lòng thử lại.";
            }
            else
            {
                ViewBag.Error = "Không nhận được phản hồi hợp lệ từ máy chủ.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            ViewBag.Error = "Đăng ký thất bại. Lỗi hệ thống.";
        }

        return View();
    }
    [HttpPost]
    public async Task<IActionResult> CreateBooking(int stationId, DateTime startTime, DateTime endTime)
    {
        try
        {
            var token = HttpContext.Session.GetString("Token");
            var sessionId = HttpContext.Session.Id;
            var sessionKeys = HttpContext.Session.Keys.ToList();

            _logger.LogInformation($"🔍 Session ID: {sessionId}");
            _logger.LogInformation($"🔍 Session Keys: {string.Join(", ", sessionKeys)}");
            _logger.LogInformation($"🔍 Token exists: {!string.IsNullOrEmpty(token)}");
            _logger.LogInformation($"🔍 Token length: {token?.Length ?? 0}");

            // Kiểm tra token - nếu null thì chuyển về login với thông báo
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("⚠️ Token not found when creating booking. Redirecting to login.");
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            _logger.LogInformation($"📤 Creating booking: StationId={stationId}, StartTime={startTime}, EndTime={endTime}");

            var bookingData = new
            {
                stationId = stationId,
                startTime = startTime,
                endTime = endTime
            };

            await _apiService.PostWithAuthAsync<object>("api/booking", bookingData, token);

            TempData["SuccessMessage"] = "Đặt chỗ thành công!";
            return RedirectToAction("Bookings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi tạo booking");
            TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            return RedirectToAction("Bookings");
        }
    }
    [HttpGet]
    public async Task<IActionResult> GetStations()
    {
        try
        {
            // Gọi API từ service
            var stations = await _apiService.GetAsync<List<StationDto>>("api/station");
            return Json(stations ?? new List<StationDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách trạm sạc");
            return Json(new List<StationDto>());
        }
    }

    // ========== 🔹 Profile ==========
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login");

        try
        {
            // Lấy thông tin user từ API
            var user = await _apiService.GetAsync<UserDto>($"api/user/{userId}");
            
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Dashboard");
            }

            ViewBag.User = user;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải thông tin profile");
            TempData["ErrorMessage"] = "Không thể tải thông tin hồ sơ.";
            return RedirectToAction("Dashboard");
        }
    }

    // ========== 🔹 Logout ==========
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    }

    // Lấy danh sách booking của user hiện tại
    public async Task<IActionResult> Bookings()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null)
            return RedirectToAction("Login");

        // Gọi API lấy danh sách bookings của người dùng
        var bookings = await _apiService.GetAsync<List<BookingDto>>($"api/booking/user/{userId}")
                       ?? new List<BookingDto>();

        // Gọi API lấy danh sách trạm sạc
        var stations = await _apiService.GetAsync<List<StationDto>>("api/station")
                       ?? new List<StationDto>();

        // Ghép tên trạm sạc vào từng booking
        foreach (var booking in bookings)
        {
            var station = stations.FirstOrDefault(s => s.Id == booking.StationId);
            booking.StationName = station?.Name ?? $"Trạm #{booking.StationId}";
        }

        // Trả model đầy đủ (có StationName) sang View
        return View(bookings);
    }

    public async Task<IActionResult> Payments()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login");

        try
        {
            // Lấy danh sách thanh toán từ Payment Service
            var payments = await _apiService.GetAsync<List<PaymentDto>>($"api/payment/user/{userId}");

            // Lấy danh sách bookings để hiển thị thông tin chi tiết
            var bookings = await _apiService.GetAsync<List<BookingDto>>($"api/booking/user/{userId}");
            var stations = await _apiService.GetAsync<List<StationDto>>("api/station");

            // Ghép thông tin booking và station vào payment
            if (payments != null && bookings != null && stations != null)
            {
                foreach (var payment in payments)
                {
                    var booking = bookings.FirstOrDefault(b => b.Id == payment.BookingId);
                    if (booking != null)
                    {
                        var station = stations.FirstOrDefault(s => s.Id == booking.StationId);
                        payment.StationName = station?.Name ?? $"Trạm #{booking.StationId}";
                    }
                }
            }

            ViewBag.Payments = payments ?? new List<PaymentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payments");
            ViewBag.Payments = new List<PaymentDto>();
        }

        return View();
    }

    
    [HttpGet]
    public async Task<IActionResult> GetPaymentDetail(int id)
    {
        try
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            var payment = await _apiService.GetAsync<PaymentDto>($"api/payment/{id}");

            if (payment == null)
            {
                return NotFound();
            }

            return Json(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment detail");
            return StatusCode(500, new { error = "Không thể lấy thông tin thanh toán" });
        }
    }






}

