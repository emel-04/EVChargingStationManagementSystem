using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace EVChargingStation.Web.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;

            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? configuration["ApiBaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                if (!baseUrl.EndsWith("/")) baseUrl += "/";
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
            else
            {
                throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
            }
        }

        private void AttachAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");

            if (string.IsNullOrWhiteSpace(token))
            {
                token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogInformation("🔑 Token attached to request header.");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _logger.LogWarning("⚠️ No token found in session. Requests may be unauthorized.");
            }
        }

        // ========== GET ==========
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                AttachAuthHeader();
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GET {Endpoint} failed: {StatusCode} - {Error}", endpoint, response.StatusCode, errorContent);
                    return default;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GET API: {Endpoint}", endpoint);
                return default;
            }
        }

        // ========== POST (CẢI TIẾN) ==========
        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                AttachAuthHeader();

                // Log dữ liệu gửi đi để debug
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation("📤 POST {Endpoint} with data:\n{Data}", endpoint, json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);

                // Đọc response content trước
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ POST {Endpoint} failed: {StatusCode}\n📋 Response: {Response}",
                        endpoint, response.StatusCode, responseContent);

                    // Throw exception với message chi tiết
                    throw new HttpRequestException(
                        $"API Error ({response.StatusCode}): {responseContent}"
                    );
                }

                _logger.LogInformation("✅ POST {Endpoint} succeeded: {StatusCode}", endpoint, response.StatusCode);

                // Nếu T là object, return null nếu response rỗng
                if (typeof(T) == typeof(object) && string.IsNullOrWhiteSpace(responseContent))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException)
            {
                // Re-throw để controller bắt được
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error calling POST API: {Endpoint}", endpoint);
                throw new HttpRequestException($"Unexpected error: {ex.Message}", ex);
            }
        }

        public async Task<T?> PostWithAuthAsync<T>(string endpoint, object data, string token)
        {
            try
            {
                // Gắn token thủ công (vì khi gọi từ controller, ta truyền token từ session)
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation("📤 [AUTH] POST {Endpoint} with data:\n{Data}", endpoint, json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ [AUTH] POST {Endpoint} failed: {StatusCode}\n📋 Response: {Response}",
                        endpoint, response.StatusCode, responseContent);

                    throw new HttpRequestException(
                        $"API Error ({response.StatusCode}): {responseContent}"
                    );
                }

                _logger.LogInformation("✅ [AUTH] POST {Endpoint} succeeded: {StatusCode}", endpoint, response.StatusCode);

                if (typeof(T) == typeof(object) && string.IsNullOrWhiteSpace(responseContent))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error calling [AUTH] POST API: {Endpoint}", endpoint);
                throw new HttpRequestException($"Unexpected error: {ex.Message}", ex);
            }
        }



        // ========== PUT (CẢI TIẾN) ==========
        public async Task<T?> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                AttachAuthHeader();

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation("📤 PUT {Endpoint} with data:\n{Data}", endpoint, json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(endpoint, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ PUT {Endpoint} failed: {StatusCode}\n📋 Response: {Response}",
                        endpoint, response.StatusCode, responseContent);

                    throw new HttpRequestException(
                        $"API Error ({response.StatusCode}): {responseContent}"
                    );
                }

                _logger.LogInformation("✅ PUT {Endpoint} succeeded: {StatusCode}", endpoint, response.StatusCode);

                if (typeof(T) == typeof(object) && string.IsNullOrWhiteSpace(responseContent))
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error calling PUT API: {Endpoint}", endpoint);
                throw new HttpRequestException($"Unexpected error: {ex.Message}", ex);
            }
        }

        // ========== DELETE ==========
        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                AttachAuthHeader();
                var response = await _httpClient.DeleteAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ DELETE {Endpoint} failed: {StatusCode} - {Error}",
                        endpoint, response.StatusCode, errorContent);
                    return false;
                }

                _logger.LogInformation("✅ DELETE {Endpoint} succeeded", endpoint);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error calling DELETE API: {Endpoint}", endpoint);
                return false;
            }
        }


        public async Task<T?> GetAsyncWithAuth<T>(string endpoint, string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }


        public async Task<byte[]> GetFileAsyncWithAuth(string endpoint, string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}