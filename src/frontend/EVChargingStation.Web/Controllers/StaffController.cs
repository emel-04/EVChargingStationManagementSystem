using EVChargingStation.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using EVChargingStation.Web.Models;

namespace EVChargingStation.Web.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ILogger<StaffController> _logger;

        public StaffController(ApiService apiService, ILogger<StaffController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // ========== 🔹 Đăng nhập Staff ==========
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
                    
                    _logger.LogInformation($"📋 Full response: {json}");

                    if (!json.TryGetProperty("user", out var user))
                    {
                        _logger.LogWarning("⚠️ No 'user' property in response");
                        ViewBag.Error = "Phản hồi từ server không hợp lệ.";
                        return View();
                    }

                    string? role = null;
                    string? firstName = "Staff";
                    int userId = 0;

                    // Lấy role
                    if (user.TryGetProperty("role", out var roleProp))
                    {
                        role = roleProp.ValueKind switch
                        {
                            JsonValueKind.String => roleProp.GetString(),
                            JsonValueKind.Number => roleProp.GetInt32().ToString(),
                            _ => null
                        };
                    }

                    // Lấy firstName
                    if (user.TryGetProperty("firstName", out var firstNameProp))
                    {
                        firstName = firstNameProp.GetString() ?? "Staff";
                    }

                    // Lấy userId
                    if (user.TryGetProperty("id", out var userIdProp))
                    {
                        userId = userIdProp.GetInt32();
                    }

                    _logger.LogInformation($"🔍 Login info - Role: {role}, FirstName: {firstName}, UserId: {userId}");

                    // Kiểm tra role là Staff (role = 1 hoặc "Staff" hoặc "staff")
                    if (!string.IsNullOrEmpty(role) && 
    (role.Equals("2") || role.Equals("CSStaff", StringComparison.OrdinalIgnoreCase)))

                    {
                        HttpContext.Session.SetString("Token", token ?? "");
                        HttpContext.Session.SetString("Role", "Staff");
                        HttpContext.Session.SetString("UserId", userId.ToString());
                        HttpContext.Session.SetString("StaffName", firstName);

                        _logger.LogInformation($"✅ Staff login successful - UserId: {userId}, Name: {firstName}");

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Invalid role for staff: {role}");
                        ViewBag.Error = "Tài khoản không có quyền nhân viên.";
                        return View();
                    }
                }

                ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi đăng nhập Staff");
                ViewBag.Error = "Lỗi hệ thống. Vui lòng thử lại.";
                return View();
            }
        }

        // ========== 🔹 Dashboard Staff ==========
        public IActionResult Index()
        {
            var token = HttpContext.Session.GetString("Token");
            var role = HttpContext.Session.GetString("Role");
            
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");
            
            // Nếu có token nhưng role khác, redirect về dashboard của role đó
            // Nhưng không xóa session, chỉ redirect
            if (role == "Admin")
                return RedirectToAction("Index", "Admin");
            
            if (role != "Staff")
                return RedirectToAction("Login");

            ViewBag.StaffName = HttpContext.Session.GetString("StaffName");
            return View();
        }

        // ========== 🔹 Quản lý Bookings ==========
        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Login");

            try
            {
                // Lấy danh sách booking
                var bookings = await _apiService.GetAsync<List<BookingDto>>("api/booking") ?? new List<BookingDto>();
                _logger.LogInformation($"📊 Số lượng booking: {bookings.Count}");

                // Lấy danh sách trạm sạc
                var stations = await _apiService.GetAsync<List<StationDto>>("api/station") ?? new List<StationDto>();

                // Lấy danh sách người dùng
                var users = await _apiService.GetAsync<List<UserDto>>("api/user") ?? new List<UserDto>();

                // THAY ĐỔI: Không gọi API payment/status/0 nữa
                // Thay vào đó, để HasPayment = false cho tất cả
                // Hoặc gọi API khác nếu cần

                // Ghép dữ liệu
                foreach (var booking in bookings)
                {
                    var station = stations.FirstOrDefault(s => s.Id == booking.StationId);
                    var user = users.FirstOrDefault(u => u.Id == booking.UserId);

                    booking.StationName = station?.Name ?? $"Trạm #{booking.StationId}";
                    if (user != null)
                    {
                        booking.UserName = $"{user.FirstName} {user.LastName}".Trim();
                    }
                    else
                    {
                        booking.UserName = $"Người dùng #{booking.UserId}";
                    }

                    // TẠM THỜI: Set HasPayment = false
                    // Sau này có thể thêm API riêng để check payment của booking
                    booking.HasPayment = false;
                }

                ViewBag.Stations = stations;
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(int stationId, DateTime startTime, DateTime endTime)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Login");

            try
            {
                var token = HttpContext.Session.GetString("Token");

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("⚠️ Token not found when creating booking.");
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

                TempData["SuccessMessage"] = "✅ Đặt chỗ thành công!";
                return RedirectToAction("Bookings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi tạo booking");
                TempData["ErrorMessage"] = $"❌ Lỗi: {ex.Message}";
                return RedirectToAction("Bookings");
            }
        }

        // ========== 🔹 Chỉnh sửa booking ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBooking(int id, int status)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Login");

            try
            {
                _logger.LogInformation($"🔄 Updating booking {id} to status {status}");

                var updateData = new
                {
                    status = status
                };

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
            if (!IsStaffLoggedIn())
                return RedirectToAction("Login");

            try
            {
                var token = HttpContext.Session.GetString("Token");
                await _apiService.PostWithAuthAsync<object>($"api/booking/{id}/cancel", new { }, token ?? "");

                TempData["SuccessMessage"] = "✅ Hủy đặt chỗ thành công!";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "❌ Lỗi hủy booking");
                TempData["ErrorMessage"] = $"❌ Không thể hủy: {ex.Message}";
            }

            return RedirectToAction("Bookings");
        }

        // ========== 🔹 API lấy danh sách stations (JSON) ==========
        [HttpGet]
        public async Task<IActionResult> GetStations()
        {
            if (!IsStaffLoggedIn())
                return Unauthorized();

            var stations = await _apiService.GetAsync<List<StationDto>>("api/station");
            return Json(stations ?? new List<StationDto>());
        }

        // ========== 🔹 Đăng xuất ==========
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ========== 🔹 Kiểm tra quyền Staff ==========
        private bool IsStaffLoggedIn()
        {
            var token = HttpContext.Session.GetString("Token");
            var role = HttpContext.Session.GetString("Role");
            
            // Kiểm tra token và role
            // Không xóa session khi role khác, chỉ return false để controller redirect
            if (string.IsNullOrEmpty(token))
                return false;
            
            // Nếu role là Staff, cho phép truy cập
            if (role == "Staff")
                return true;
            
            // Nếu có token nhưng role khác (Admin, User, etc), không cho phép truy cập
            // Nhưng không xóa session để giữ thông tin đăng nhập
            return false;
        }

        // ========== 🔹 Danh sách Payments ==========
        // ========== 🔹 Danh sách Payments ==========
        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Login");

            try
            {
                // THAY ĐỔI: Không lọc theo status nữa, lấy tất cả
                // Lấy tất cả payments - KHÔNG dùng /status/0
                List<PaymentDto> payments = new List<PaymentDto>();

                try
                {
                    // Thử gọi API lấy tất cả payments
                    // Bạn cần thêm endpoint GET /api/payment trong PaymentController
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

                // Chỉ lấy payments có status = 0 (Pending) để hiển thị
                var pendingPayments = payments.Where(p => p.Status == 1).ToList();

                return View(pendingPayments);
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
            if (!IsStaffLoggedIn())
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

        // ========== 🔹 Tạo thanh toán cho booking hoàn thành ==========
        // ========== 🔹 Tạo thanh toán cho booking hoàn thành ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(int bookingId, decimal amount, int method, string? description)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Login");

            try
            {
                var token = HttpContext.Session.GetString("Token");

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("⚠️ Token not found when creating payment.");
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login");
                }

                // ✅ BƯỚ C 1: Lấy thông tin booking để lấy UserId
                var booking = await _apiService.GetAsync<BookingDto>($"api/booking/{bookingId}");

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "❌ Không tìm thấy booking.";
                    return RedirectToAction("Bookings");
                }

                // Kiểm tra booking đã hoàn thành chưa
                if (booking.Status != 3)
                {
                    TempData["ErrorMessage"] = "❌ Chỉ có thể tạo thanh toán cho booking đã hoàn thành.";
                    return RedirectToAction("Bookings");
                }

                _logger.LogInformation($"📤 Creating payment for booking: {bookingId}, UserId: {booking.UserId}, Amount: {amount}");

                // ✅ BƯỚC 2: Gọi API POST /api/payment với đầy đủ thông tin
                var paymentData = new
                {
                    userId = booking.UserId,  // ← Lấy từ booking
                    bookingId = bookingId,
                    amount = amount,
                    method = method,
                    description = description ?? $"Thanh toán cho booking #{booking.BookingNumber}"
                };

                await _apiService.PostWithAuthAsync<object>("api/payment", paymentData, token);

                TempData["SuccessMessage"] = "✅ Tạo thanh toán thành công!";
                _logger.LogInformation($"✅ Payment created successfully for booking {bookingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi tạo thanh toán");
                TempData["ErrorMessage"] = $"❌ Không thể tạo thanh toán: {ex.Message}";
            }

            return RedirectToAction("Bookings");
        }

        // ========== 🔹 Báo cáo tháng ==========
        [HttpGet]
        public async Task<IActionResult> MonthlyReport(DateTime? fromDate, DateTime? toDate)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Login");

            try
            {
                // Nếu không có tham số, dùng tháng hiện tại
                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    var now = DateTime.UtcNow;
                    fromDate = new DateTime(now.Year, now.Month, 1);
                    toDate = fromDate.Value.AddMonths(1).AddDays(-1);
                }

                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tải trang báo cáo.");
                TempData["ErrorMessage"] = "Không thể tải trang báo cáo.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(DateTime fromDate, DateTime toDate)
        {
            if (!IsStaffLoggedIn())
                return RedirectToAction("Login");

            try
            {
                var token = HttpContext.Session.GetString("Token");

                _logger.LogInformation($"📊 Tạo báo cáo từ {fromDate:yyyy-MM-dd} đến {toDate:yyyy-MM-dd}");

                // Lấy tất cả bookings
                var allBookings = await _apiService.GetAsync<List<BookingDto>>("api/booking") ?? new List<BookingDto>();
                
                _logger.LogInformation($"📋 Tổng số booking trong hệ thống: {allBookings.Count}");

                // Lọc bookings trong khoảng thời gian
                var bookingsInRange = allBookings
                    .Where(b => b.StartTime >= fromDate && b.StartTime <= toDate)
                    .ToList();

                _logger.LogInformation($"📋 Booking trong khoảng thời gian: {bookingsInRange.Count}");

                // Đếm số lượng user unique
                var uniqueUsers = bookingsInRange
                    .Select(b => b.UserId)
                    .Distinct()
                    .Count();

                // Tổng số bookings
                var totalBookings = bookingsInRange.Count;

                // Tính tổng doanh thu từ payments
                var payments = await _apiService.GetAsync<List<PaymentDto>>("api/payment") ?? new List<PaymentDto>();
                
                _logger.LogInformation($"💰 Tổng số payment trong hệ thống: {payments.Count}");

                var paymentsInRange = payments
                    .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate && p.Status == 1)
                    .ToList();

                _logger.LogInformation($"💰 Payment trong khoảng thời gian (status=1): {paymentsInRange.Count}");

                var totalRevenue = paymentsInRange.Sum(p => p.Amount);

                ViewBag.FromDate = fromDate.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate.ToString("yyyy-MM-dd");
                ViewBag.DisplayPeriod = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
                ViewBag.TotalBookings = totalBookings;
                ViewBag.UniqueUsers = uniqueUsers;
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.CompletedBookings = bookingsInRange.Count(b => b.Status == 3);
                ViewBag.CancelledBookings = bookingsInRange.Count(b => b.Status == 4);
                ViewBag.TotalPayments = paymentsInRange.Count;

                _logger.LogInformation($"✅ Báo cáo: {totalBookings} bookings, {uniqueUsers} users, {totalRevenue:N0} VNĐ");

                return View("MonthlyReport");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tạo báo cáo.");
                TempData["ErrorMessage"] = $"Không thể tạo báo cáo: {ex.Message}";
                
                ViewBag.FromDate = fromDate.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate.ToString("yyyy-MM-dd");
                
                return View("MonthlyReport");
            }
        }
    }
}