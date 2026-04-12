using EVChargingStation.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using EVChargingStation.Web.Models;

namespace EVChargingStation.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApiService apiService, ILogger<AdminController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // ========== 🔹 Đăng nhập ==========
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var data = new { email, password };
                var result = await _apiService.PostAsync<object>("api/auth/login", data);

                if (result is JsonElement json && json.TryGetProperty("token", out var tokenProp))
                {
                    var token = tokenProp.GetString();
                    var user = json.GetProperty("user");
                    string? role = null;

                    if (user.TryGetProperty("role", out var roleProp))
                    {
                        role = roleProp.ValueKind switch
                        {
                            JsonValueKind.String => roleProp.GetString(),
                            JsonValueKind.Number => roleProp.GetInt32().ToString(),
                            _ => null
                        };
                    }

                    if (!string.IsNullOrEmpty(role) && (role.ToLower().Contains("admin") || role == "2" || role == "3"))
                    {
                        HttpContext.Session.SetString("Token", token ?? "");
                        HttpContext.Session.SetString("Role", "Admin");
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.Error = "Tài khoản không có quyền quản trị.";
                        return View();
                    }
                }

                ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đăng nhập Admin");
                ViewBag.Error = "Lỗi hệ thống. Vui lòng thử lại.";
                return View();
            }
        }

        // ========== 🔹 Dashboard ==========
        public IActionResult Index()
        {
            var token = HttpContext.Session.GetString("Token");
            var role = HttpContext.Session.GetString("Role");
            
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");
            
            // Nếu có token nhưng role khác, redirect về dashboard của role đó
            // Nhưng không xóa session, chỉ redirect
            if (role == "Staff")
                return RedirectToAction("Index", "Staff");
            
            if (role != "Admin")
                return RedirectToAction("Login");
                
            return View();
        }

        // ========== 🔹 Danh sách trạm ==========
        public async Task<IActionResult> Stations()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            var stations = await _apiService.GetAsync<List<StationDto>>("api/station");
            ViewBag.Stations = stations ?? new List<StationDto>();
            return View();
        }

        // ========== 🔹 Thêm trạm ==========
        [HttpGet]
        public IActionResult CreateStation()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStation(string name, string address, int numberOfPorts, double powerKW)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            try
            {
                var newStation = new
                {
                    name,
                    address,
                    numberOfPorts,
                    powerKW
                };

                await _apiService.PostAsync<object>("api/station", newStation);
                TempData["Message"] = "✅ Thêm trạm sạc thành công!";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to create station");
                TempData["Error"] = $"❌ Không thể thêm trạm: {ex.Message}";
            }

            return RedirectToAction("Stations");
        }

        // ========== 🔹 Sửa trạm ==========
        [HttpGet]
        public async Task<IActionResult> EditStation(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            var station = await _apiService.GetAsync<StationDto>($"api/station/{id}");
            if (station == null)
            {
                TempData["Error"] = "Không tìm thấy trạm sạc.";
                return RedirectToAction("Stations");
            }

            return View(station);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStation(StationDto station)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            _logger.LogInformation("📥 Dữ liệu nhận từ form EditStation: {@Station}", station);

            try
            {
                await _apiService.PutAsync<object>($"api/station/{station.Id}", station);
                TempData["Message"] = "✅ Cập nhật trạm sạc thành công!";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to update station {Id}", station.Id);
                TempData["Error"] = $"❌ Cập nhật thất bại: {ex.Message}";
            }

            return RedirectToAction("Stations");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStation(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            bool deleted = await _apiService.DeleteAsync($"api/station/{id}");
            TempData[deleted ? "Message" : "Error"] = deleted
                ? "✅ Xóa trạm sạc thành công!"
                : "⚠️ Xóa trạm sạc thất bại.";

            return RedirectToAction("Stations");
        }

        // ========== 🔹 Danh sách người dùng ==========
        public async Task<IActionResult> Users()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            var users = await _apiService.GetAsync<List<UserDto>>("api/user");
            ViewBag.Users = users ?? new List<UserDto>();
            return View();
        }

        // ========== 🔹 Thêm người dùng ==========
        [HttpGet]
        public IActionResult CreateUser()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            return View(new UserDto { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    int Role,
    bool IsActive)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            _logger.LogInformation("🔄 Creating user: Email={Email}, Role={Role}", Email, Role);

            try
            {
                var model = new
                {
                    FirstName,
                    LastName,
                    Email,
                    PhoneNumber,
                    Password,
                    Role,
                    IsActive
                };

                await _apiService.PostAsync<object>("api/user", model);

                TempData["Message"] = "✅ Thêm người dùng thành công!";
                return RedirectToAction("Users");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Failed to create user");
                TempData["Error"] = $"❌ Lỗi: {ex.Message}";
                return View();
            }
        }

        // ========== 🔹 Sửa người dùng ==========
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            var user = await _apiService.GetAsync<UserDto>($"api/user/{id}");
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Users");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(UserDto user)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu người dùng.";
                return RedirectToAction("Users");
            }

            try
            {
                var payload = new
                {
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    role = user.Role,
                    isActive = user.IsActive
                };

                await _apiService.PutAsync<object>($"api/user/{user.Id}", payload);
                TempData["Message"] = "✅ Cập nhật người dùng thành công!";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to update user {Id}", user.Id);
                TempData["Error"] = $"❌ Cập nhật thất bại: {ex.Message}";
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            bool deleted = await _apiService.DeleteAsync($"api/user/{id}");
            TempData[deleted ? "Message" : "Error"] = deleted
                ? "✅ Xóa người dùng thành công!"
                : "⚠️ Xóa thất bại!";

            return RedirectToAction("Users");
        }

        // ========== 🔹 Đăng xuất ==========
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ========== 🔹 Kiểm tra quyền Admin ==========
        private bool IsAdminLoggedIn()
        {
            var token = HttpContext.Session.GetString("Token");
            var role = HttpContext.Session.GetString("Role");
            
            // Kiểm tra token và role
            // Không xóa session khi role khác, chỉ return false để controller redirect
            if (string.IsNullOrEmpty(token))
                return false;
            
            // Nếu role là Admin, cho phép truy cập
            if (role == "Admin")
                return true;
            
            // Nếu có token nhưng role khác (Staff, User, etc), không cho phép truy cập
            // Nhưng không xóa session để giữ thông tin đăng nhập
            return false;
        }

        // ========== 🔹 Danh sách booking ========== 
        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            try
            {
                // 🔹 Gọi API để lấy danh sách booking
                var bookings = await _apiService.GetAsync<List<BookingDto>>("api/booking") ?? new List<BookingDto>();
                _logger.LogInformation($"📊 Số lượng booking: {bookings.Count}");
                  if (bookings.Count == 0)
        {
            _logger.LogWarning("⚠️ API trả về 0 booking nhưng DB có dữ liệu!");
            _logger.LogWarning("🔍 Kiểm tra API Backend có đang lọc theo UserId không?");
        }
        else
        {
            _logger.LogInformation($"✅ Danh sách booking: {string.Join(", ", bookings.Select(b => b.BookingNumber))}");
        }

                // 🔹 Gọi API lấy danh sách trạm sạc
                var stations = await _apiService.GetAsync<List<StationDto>>("api/station") ?? new List<StationDto>();

                // 🔹 Gọi API lấy danh sách người dùng
                var users = await _apiService.GetAsync<List<UserDto>>("api/user") ?? new List<UserDto>();

                // 🔹 Ghép dữ liệu để hiển thị tên người dùng và tên trạm
                foreach (var booking in bookings)
                {
                    var station = stations.FirstOrDefault(s => s.Id == booking.StationId);
                    var user = users.FirstOrDefault(u => u.Id == booking.UserId);

                    booking.StationName = station?.Name ?? $"Trạm #{booking.StationId}";
                    booking.UserName = user?.FullName ?? $"Người dùng #{booking.UserId}";
                }

                // 🔹 Truyền model sang View
                return View(bookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tải danh sách booking.");
                TempData["ErrorMessage"] = "Không thể tải danh sách đặt chỗ.";
                return View(new List<BookingDto>());
            }
        }
        // ========== 🔹 Tạo booking mới ==========
        // ========== 🔹 Tạo booking mới (CHỈ CHỌN TRẠM) ==========
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

        // ========== 🔹 Chỉnh sửa booking ==========
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditBooking(int id, int status)
{
    if (!IsAdminLoggedIn())
        return RedirectToAction("Login");

    try
    {
        _logger.LogInformation($"🔄 Updating booking {id} to status {status}");

        var updateData = new
        {
            status = status  // Chỉ cập nhật status
        };

        var token = HttpContext.Session.GetString("Token");
        await _apiService.PutAsync<object>($"api/booking/{id}", updateData);

        TempData["SuccessMessage"] = "✅ Cập nhật trạng thái booking thành công!";
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "❌ Lỗi cập nhật booking");
        TempData["ErrorMessage"] = $"❌ Không thể cập nhật: {ex.Message}";
    }

    return RedirectToAction("Bookings");
}

        // ========== 🔹 Hủy booking ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            try
            {
                var token = HttpContext.Session.GetString("Token");
                await _apiService.PostWithAuthAsync<object>($"api/booking/{id}/cancel", new { }, token ?? "");

                TempData["Message"] = "✅ Hủy đặt chỗ thành công!";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Lỗi hủy booking");
                TempData["Error"] = $"❌ Không thể hủy: {ex.Message}";
            }

            return RedirectToAction("Bookings");
        }

        // ========== 🔹 API lấy danh sách users (JSON) ==========
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            if (!IsAdminLoggedIn())
                return Unauthorized();

            var users = await _apiService.GetAsync<List<UserDto>>("api/user");
            return Json(users ?? new List<UserDto>());
        }

        // ========== 🔹 API lấy danh sách stations (JSON) ==========
        [HttpGet]
        public async Task<IActionResult> GetStations()
        {
            if (!IsAdminLoggedIn())
                return Unauthorized();

            var stations = await _apiService.GetAsync<List<StationDto>>("api/station");
            return Json(stations ?? new List<StationDto>());
        }

        // ========== 🔹 API lấy charging points theo station ==========
        [HttpGet]
        public async Task<IActionResult> GetChargingPoints(int stationId)
        {
            if (!IsAdminLoggedIn())
                return Unauthorized();

            try
            {
                var points = await _apiService.GetAsync<List<ChargingPointDto>>($"api/chargingpoint/station/{stationId}");
                return Json(points ?? new List<ChargingPointDto>());
            }
            catch
            {
                return Json(new List<ChargingPointDto>());
            }
        }

        // ========== 🔹 Kiểm tra trụ sạc có đang bận không ==========
        [HttpGet]
        public async Task<IActionResult> CheckChargingPoint(int chargingPointId)
        {
            if (!IsAdminLoggedIn())
                return Unauthorized();

            try
            {
                var booking = await _apiService.GetAsync<BookingDto>($"api/booking/active/charging-point/{chargingPointId}");

                if (booking != null)
                {
                    return Json(new { isOccupied = true, bookingNumber = booking.BookingNumber });
                }

                return Json(new { isOccupied = false });
            }
            catch
            {
                return Json(new { isOccupied = false });
            }
        }

        // ========== 🔹 Báo cáo với biểu đồ ==========
[HttpGet]
public async Task<IActionResult> Reports()
{
    if (!IsAdminLoggedIn())
        return RedirectToAction("Login");

    try
    {
        // Lấy tháng hiện tại
        var now = DateTime.UtcNow;
        var fromDate = new DateTime(now.Year, now.Month, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);

        ViewBag.FromDate = fromDate.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate.ToString("yyyy-MM-dd");

        return View();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Lỗi khi tải trang báo cáo.");
        TempData["ErrorMessage"] = "Không thể tải trang báo cáo.";
        return RedirectToAction("Index");
    }
}

        // ========== 🔹 API lấy dữ liệu biểu đồ ==========
        [HttpGet]
        public async Task<IActionResult> GetChartData(DateTime fromDate, DateTime toDate)
        {
            if (!IsAdminLoggedIn())
                return Unauthorized();

            try
            {
                _logger.LogInformation($"📊 Tạo báo cáo biểu đồ từ {fromDate:yyyy-MM-dd} đến {toDate:yyyy-MM-dd}");

                // Lấy tất cả bookings
                var allBookings = await _apiService.GetAsync<List<BookingDto>>("api/booking") ?? new List<BookingDto>();

                _logger.LogInformation($"📋 Tổng số booking trong hệ thống: {allBookings.Count}");

                // Lọc bookings trong khoảng thời gian
                var bookingsInRange = allBookings
                    .Where(b => b.StartTime >= fromDate && b.StartTime <= toDate)
                    .ToList();

                _logger.LogInformation($"📋 Booking trong khoảng thời gian: {bookingsInRange.Count}");

                // Lấy payments
                var payments = await _apiService.GetAsync<List<PaymentDto>>("api/payment") ?? new List<PaymentDto>();

                _logger.LogInformation($"💰 Tổng số payment trong hệ thống: {payments.Count}");

                var paymentsInRange = payments
                    .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate && p.Status == 1)
                    .ToList();

                _logger.LogInformation($"💰 Payment trong khoảng thời gian (status=1): {paymentsInRange.Count}");

                // Thống kê theo ngày
                var dailyStats = new List<object>();
                for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
                {
                    var dayBookings = bookingsInRange.Where(b => b.StartTime.Date == date).Count();
                    var dayRevenue = paymentsInRange
                        .Where(p => p.CreatedAt.Date == date)
                        .Sum(p => p.Amount);

                    dailyStats.Add(new
                    {
                        date = date.ToString("dd/MM"),
                        bookings = dayBookings,
                        revenue = dayRevenue
                    });
                }

                // Thống kê theo trạng thái
                var statusStats = new
                {
                    pending = bookingsInRange.Count(b => b.Status == 0),
                    confirmed = bookingsInRange.Count(b => b.Status == 1),
                    checkedIn = bookingsInRange.Count(b => b.Status == 2),
                    completed = bookingsInRange.Count(b => b.Status == 3),
                    cancelled = bookingsInRange.Count(b => b.Status == 4)
                };

                // Lấy danh sách stations để mapping
                var stations = await _apiService.GetAsync<List<StationDto>>("api/station") ?? new List<StationDto>();

                // Thống kê theo trạm
                var stationStats = bookingsInRange
                    .GroupBy(b => b.StationId)
                    .Select(g =>
                    {
                        var station = stations.FirstOrDefault(s => s.Id == g.Key);
                        var stationBookingIds = g.Select(b => b.Id).ToList();
                        var stationRevenue = paymentsInRange
                            .Where(p => p.BookingId.HasValue && stationBookingIds.Contains(p.BookingId.Value))
                            .Sum(p => p.Amount);

                        return new
                        {
                            stationId = g.Key,
                            stationName = station?.Name ?? $"Trạm {g.Key}",
                            bookings = g.Count(),
                            revenue = stationRevenue
                        };
                    })
                    .OrderByDescending(x => x.bookings)
                    .Take(10)
                    .ToList();

                // Tổng quan
                var overview = new
                {
                    totalBookings = bookingsInRange.Count,
                    totalRevenue = paymentsInRange.Sum(p => p.Amount),
                    totalUsers = bookingsInRange.Select(b => b.UserId).Distinct().Count(),
                    totalPayments = paymentsInRange.Count,
                    avgBookingPerDay = dailyStats.Count > 0 ? bookingsInRange.Count / (double)dailyStats.Count : 0,
                    avgRevenuePerDay = dailyStats.Count > 0 ? paymentsInRange.Sum(p => p.Amount) / dailyStats.Count : 0
                };

                _logger.LogInformation($"✅ Báo cáo: {overview.totalBookings} bookings, {overview.totalUsers} users, {overview.totalRevenue:N0} VNĐ");

                return Json(new
                {
                    success = true,
                    overview,
                    dailyStats,
                    statusStats,
                    stationStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi lấy dữ liệu biểu đồ.");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ========== 🔹 Danh sách Payments ==========
        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            try
            {
                // Lấy tất cả payments
                List<PaymentDto> payments = new List<PaymentDto>();

                try
                {
                    payments = await _apiService.GetAsync<List<PaymentDto>>("api/payment") ?? new List<PaymentDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Không thể lấy danh sách payments");
                }

                // Lấy danh sách users
                var users = await _apiService.GetAsync<List<UserDto>>("api/user") ?? new List<UserDto>();

                // Ghép thông tin user
                foreach (var payment in payments)
                {
                    var user = users.FirstOrDefault(u => u.Id == payment.UserId);
                    if (user != null)
                    {
                        payment.UserName = $"{user.FirstName} {user.LastName}".Trim();
                    }
                    else
                    {
                        payment.UserName = $"Người dùng #{payment.UserId}";
                    }
                }

                return View(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tải danh sách thanh toán.");
                TempData["ErrorMessage"] = "Không thể tải danh sách thanh toán.";
                return View(new List<PaymentDto>());
            }
        }

        // ========== 🔹 Xử lý Payment ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int id, int status, string? transactionId)
        {
            if (!IsAdminLoggedIn())
                return RedirectToAction("Login");

            try
            {
                var token = HttpContext.Session.GetString("Token");

                var processData = new
                {
                    status = status,
                    transactionId = transactionId ?? "",
                    description = status == 1 ? "Thanh toán thành công" : "Thanh toán thất bại"
                };

                await _apiService.PostWithAuthAsync<object>($"api/payment/{id}/process", processData, token ?? "");

                TempData["SuccessMessage"] = "✅ Xử lý thanh toán thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi xử lý thanh toán");
                TempData["ErrorMessage"] = $"❌ Không thể xử lý: {ex.Message}";
            }

            return RedirectToAction("Payments");
        }







    }
     
     
}