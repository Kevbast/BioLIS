using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
// Usings de la librería compartida.
using BioLIS.Models.Entities;
using BioLIS.Models.Common;
using BioLIS.Models.DTOs.Auth;
using BioLIS.Models.DTOs.Orders;
using BioLIS.Models.DTOs.Portal;
using BioLIS.Models.DTOs.Common; // <-- ESTE ES EL QUE FALTABA

namespace BioLIS.Services
{
    // Servicio HTTP para la ApiBioLIS usando JWT en el claim "TOKEN".
    public class ApiService
    {
        private readonly string urlApi;
        private readonly MediaTypeWithQualityHeaderValue header;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IConfiguration configuration;

        public ApiService(IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            this.configuration   = configuration;
            this.contextAccessor = contextAccessor;
            this.urlApi          = configuration.GetValue<string>("ApiSettings:BaseUrl")!;
            this.header          = new MediaTypeWithQualityHeaderValue("application/json");
        }

        // Lee el token JWT del claim "TOKEN".

        private string? GetToken()
            => this.contextAccessor.HttpContext?.User
                   .FindFirst(z => z.Type == "TOKEN")?.Value?.Trim();

        private static User MapApiUser(ApiUserDto dto)
        {
            int roleId = dto.RoleID;
            string? roleName = null;

            if (dto.Role != null)
            {
                if (dto.Role.Type == JTokenType.String)
                {
                    roleName = dto.Role.Value<string>();
                }
                else if (dto.Role.Type == JTokenType.Object)
                {
                    roleName = dto.Role["roleName"]?.Value<string>() ?? dto.Role["RoleName"]?.Value<string>();
                    roleId = dto.Role["roleID"]?.Value<int?>()
                        ?? dto.Role["RoleID"]?.Value<int?>()
                        ?? roleId;
                }
            }

            roleName ??= dto.RoleName;
            if (roleId == 0) roleId = GetRoleIdByName(roleName);
            if (string.IsNullOrWhiteSpace(roleName)) roleName = GetRoleNameById(roleId);

            return new User
            {
                UserID = dto.UserID,
                Username = dto.Username,
                Email = dto.Email,
                PhotoFilename = dto.PhotoFilename,
                PasswordText = string.Empty,
                RoleID = roleId,
                Role = roleId > 0 || !string.IsNullOrWhiteSpace(roleName)
                    ? new Role { RoleID = roleId, RoleName = roleName ?? string.Empty }
                    : null,
                DoctorID = dto.DoctorID,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt ?? DateTime.Now
            };
        }

        private static int GetRoleIdByName(string? roleName)
            => roleName switch
            {
                "Admin" => 1,
                "Laboratorio" => 2,
                "Doctor" => 3,
                _ => 0
            };

        private static string GetRoleNameById(int roleId)
            => roleId switch
            {
                1 => "Admin",
                2 => "Laboratorio",
                3 => "Doctor",
                _ => string.Empty
            };

        // Métodos HTTP privados genéricos.

        private async Task<T?> CallApiAsync<T>(string request)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            HttpResponseMessage response = await client.GetAsync(request);
            if (!response.IsSuccessStatusCode) return default;
            string data = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(data);
        }

        private async Task<T?> CallApiAsync<T>(string request, string token)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response = await client.GetAsync(request);
            if (!response.IsSuccessStatusCode) return default;
            string data = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(data);
        }

        private async Task<ApiResult> PostApiAsync<T>(string request, T body, string? token = null)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string json       = JsonConvert.SerializeObject(body);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(request, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ApiResult(response.IsSuccessStatusCode, responseBody);
        }

        private async Task<ApiResult> PostMultipartAsync(string request, MultipartFormDataContent content, string? token = null)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PostAsync(request, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ApiResult(response.IsSuccessStatusCode, responseBody);
        }

        private async Task<ApiResult> PutMultipartAsync(string request, MultipartFormDataContent content, string? token = null)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.PutAsync(request, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ApiResult(response.IsSuccessStatusCode, responseBody);
        }

        private static void AddFormField(MultipartFormDataContent content, string name, string? value)
        {
            if (value == null) return;
            content.Add(new StringContent(value), name);
        }

        private static async Task AddFormFileAsync(
            MultipartFormDataContent content, string name, IFormFile file)
        {
            var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var sc = new StreamContent(ms);
            if (!string.IsNullOrWhiteSpace(file.ContentType))
                sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(sc, name, file.FileName);
        }

        private async Task<ApiResult> PutApiAsync<T>(string request, T body, string? token = null)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string json       = JsonConvert.SerializeObject(body);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PutAsync(request, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ApiResult(response.IsSuccessStatusCode, responseBody);
        }

        private async Task<ApiResult> PatchApiAsync<T>(string request, T body, string? token = null)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string json       = JsonConvert.SerializeObject(body);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PatchAsync(request, content);
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ApiResult(response.IsSuccessStatusCode, responseBody);
        }

        private async Task<ApiResult> DeleteApiAsync(string request, string? token = null)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await client.DeleteAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            return new ApiResult(response.IsSuccessStatusCode, responseBody);
        }

        // AUTH

        /// Login: devuelve el token JWT crudo o null si las credenciales son incorrectas.
        public async Task<string?> LoginAsync(string username, string password)
        {
            using HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(this.urlApi);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(this.header);

            var model   = new { username, password };
            string json = JsonConvert.SerializeObject(model);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/api/auth/login", content);
            if (!response.IsSuccessStatusCode) return null;

            string data     = await response.Content.ReadAsStringAsync();
            dynamic? parsed = JsonConvert.DeserializeObject<dynamic>(data);
            return parsed?.response?.ToString();
        }

        public async Task<User?> GetProfileAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            var dto = await CallApiAsync<ApiUserDto>("/api/auth/profile", token);
            return dto == null ? null : MapApiUser(dto);
        }

        /// Versión usada en Login cuando no hay cookie ni claim activo.
        public async Task<User?> GetProfileWithTokenAsync(string token)
        {
            var dto = await CallApiAsync<ApiUserDto>("/api/auth/profile", token);
            return dto == null ? null : MapApiUser(dto);
        }

        // Usuarios

        public async Task<List<User>?> GetAllUsersAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            var users = await CallApiAsync<List<ApiUserDto>>("/api/auth/users", token);
            return users?.Select(MapApiUser).ToList();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            string? token = GetToken();
            if (token == null) return null;
            var dto = await CallApiAsync<ApiUserDto>($"/api/auth/users/{id}", token);
            return dto == null ? null : MapApiUser(dto);
        }

        public async Task<List<User>?> GetInactiveUsersAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            var users = await CallApiAsync<List<ApiUserDto>>("/api/auth/users/inactive", token);
            return users?.Select(MapApiUser).ToList();
        }

        public async Task<List<User>?> GetUsersByRoleAsync(string role)
        {
            string? token = GetToken();
            if (token == null) return null;
            var users = await CallApiAsync<List<ApiUserDto>>($"/api/auth/users/by-role/{role}", token);
            return users?.Select(MapApiUser).ToList();
        }

        public async Task<ApiResult> CreateUserAsync(string username, string password, string role,
            string? email, string? photoFilename, int? doctorId, IFormFile? photoFile)
        {
            string? token = GetToken();
            using var content = new MultipartFormDataContent();
            AddFormField(content, "Username", username);
            AddFormField(content, "Password", password);
            AddFormField(content, "RoleName", role);
            AddFormField(content, "Email", email);
            AddFormField(content, "PhotoFilename", photoFilename);
            AddFormField(content, "DoctorId", doctorId?.ToString());
            if (photoFile is { Length: > 0 })
                await AddFormFileAsync(content, "Photo", photoFile);

            return await PostMultipartAsync("/api/auth/users", content, token);
        }

        public async Task<ApiResult> UpdateUserAsync(int id, string username, string? email,
            string? photoFilename, IFormFile? photoFile, string? newPassword, string roleName, int? doctorId)
        {
            string? token = GetToken();
            using var content = new MultipartFormDataContent();
            AddFormField(content, "Username", username);
            AddFormField(content, "Email", email);
            AddFormField(content, "PhotoFilename", photoFilename);
            AddFormField(content, "NewPassword", newPassword);
            AddFormField(content, "RoleName", roleName);
            AddFormField(content, "DoctorId", doctorId?.ToString());
            if (photoFile is { Length: > 0 })
                await AddFormFileAsync(content, "Photo", photoFile);

            return await PutMultipartAsync($"/api/auth/users/{id}", content, token);
        }

        public async Task<ApiResult> UpdateMyProfileAsync(string username, string? email, string? photoFilename, IFormFile? photoFile)
        {
            string? token = GetToken();
            using var content = new MultipartFormDataContent();
            AddFormField(content, "Username", username);
            AddFormField(content, "Email", email);
            AddFormField(content, "PhotoFilename", photoFilename);
            if (photoFile is { Length: > 0 })
                await AddFormFileAsync(content, "Photo", photoFile);

            return await PutMultipartAsync("/api/auth/users/me", content, token);
        }

        public async Task<ApiResult> DeleteUserAsync(int id)
        {
            string? token = GetToken();
            return await DeleteApiAsync($"/api/auth/users/{id}", token);
        }

        public async Task<ApiResult> ReactivateUserAsync(int id)
        {
            string? token = GetToken();
            return await PostApiAsync<object>($"/api/auth/users/{id}/reactivate", new { }, token);
        }

        public async Task<ApiResult> ChangePasswordAsync(int id, string currentPassword, string newPassword)
        {
            string? token = GetToken();
            return await PostApiAsync($"/api/auth/users/{id}/change-password",
                new { currentPassword, newPassword }, token);
        }

        public async Task<Dictionary<string, int>?> GetUserStatsByRoleAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<Dictionary<string, int>>("/api/auth/stats/by-role", token);
        }

        // Pacientes

        public async Task<List<Patient>?> GetPatientsAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Patient>>("/api/patients", token);
        }

        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<Patient>($"/api/patients/{id}", token);
        }

        public async Task<List<Patient>?> SearchPatientsAsync(string term)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Patient>>(
                $"/api/patients/search?term={Uri.EscapeDataString(term)}", token);
        }

        public async Task<List<Patient>?> GetInactivePatientsAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Patient>>("/api/patients/inactive", token);
        }

        public async Task<ApiResult> CreatePatientAsync(string firstName, string lastName, string gender,
            DateTime birthDate, string? email, string? photoFilename, string? phoneNumber, IFormFile? photoFile)
        {
            string? token = GetToken();
            using var content = new MultipartFormDataContent();
            AddFormField(content, "FirstName", firstName);
            AddFormField(content, "LastName", lastName);
            AddFormField(content, "Gender", gender);
            AddFormField(content, "BirthDate", birthDate.ToString("o"));
            AddFormField(content, "Email", email);
            AddFormField(content, "PhotoFilename", photoFilename);
            AddFormField(content, "PhoneNumber", phoneNumber);
            if (photoFile is { Length: > 0 })
                await AddFormFileAsync(content, "Photo", photoFile);

            return await PostMultipartAsync("/api/patients", content, token);
        }

        public async Task<ApiResult> UpdatePatientAsync(int id, string firstName, string lastName,
            string gender, DateTime birthDate, string? email, string? photoFilename, string? phoneNumber, IFormFile? photoFile)
        {
            string? token = GetToken();
            using var content = new MultipartFormDataContent();
            AddFormField(content, "FirstName", firstName);
            AddFormField(content, "LastName", lastName);
            AddFormField(content, "Gender", gender);
            AddFormField(content, "BirthDate", birthDate.ToString("o"));
            AddFormField(content, "Email", email);
            AddFormField(content, "PhotoFilename", photoFilename);
            AddFormField(content, "PhoneNumber", phoneNumber);
            if (photoFile is { Length: > 0 })
                await AddFormFileAsync(content, "Photo", photoFile);

            return await PutMultipartAsync($"/api/patients/{id}", content, token);
        }

        public async Task<ApiResult> DeletePatientAsync(int id)
        {
            string? token = GetToken();
            return await DeleteApiAsync($"/api/patients/{id}", token);
        }

        public async Task<ApiResult> ReactivatePatientAsync(int id)
        {
            string? token = GetToken();
            return await PostApiAsync<object>($"/api/patients/{id}/reactivate", new { }, token);
        }

        public async Task<List<TestResult>?> GetPatientHistoryAsync(int patientId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<TestResult>>($"/api/patients/{patientId}/history", token);
        }

        // Doctores

        public async Task<List<Doctor>?> GetDoctorsAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Doctor>>("/api/doctors", token);
        }

        public async Task<Doctor?> GetDoctorByIdAsync(int id)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<Doctor>($"/api/doctors/{id}", token);
        }

        public async Task<List<Doctor>?> GetInactiveDoctorsAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Doctor>>("/api/doctors/inactive", token);
        }

        public async Task<List<Doctor>?> GetDoctorsWithoutUserAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Doctor>>("/api/doctors/without-user", token);
        }

        public async Task<ApiResult> CreateDoctorAsync(string fullName, string licenseNumber,
            string? email, string? phoneNumber)
        {
            string? token = GetToken();
            return await PostApiAsync("/api/doctors",
                new { fullName, licenseNumber, email, phoneNumber }, token);
        }

        public async Task<ApiResult> UpdateDoctorAsync(int id, string fullName, string licenseNumber,
            string? email, string? phoneNumber)
        {
            string? token = GetToken();
            return await PutApiAsync($"/api/doctors/{id}",
                new { fullName, licenseNumber, email, phoneNumber }, token);
        }

        public async Task<ApiResult> DeleteDoctorAsync(int id)
        {
            string? token = GetToken();
            return await DeleteApiAsync($"/api/doctors/{id}", token);
        }

        public async Task<ApiResult> ReactivateDoctorAsync(int id)
        {
            string? token = GetToken();
            return await PostApiAsync<object>($"/api/doctors/{id}/reactivate", new { }, token);
        }

        // Sample types

        public async Task<List<SampleType>?> GetSampleTypesAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<SampleType>>("/api/sampletypes", token);
        }

        public async Task<SampleType?> GetSampleTypeByIdAsync(int id)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<SampleType>($"/api/sampletypes/{id}", token);
        }

        public async Task<List<SampleType>?> GetInactiveSampleTypesAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<SampleType>>("/api/sampletypes/inactive", token);
        }

        public async Task<List<LabTest>?> GetLabTestsBySampleTypeAsync(int sampleId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<LabTest>>($"/api/labtests/by-sample/{sampleId}", token);
        }

        public async Task<ApiResult> CreateSampleTypeAsync(string sampleName, string? containerColor)
        {
            string? token = GetToken();
            return await PostApiAsync("/api/sampletypes", new { sampleName, containerColor }, token);
        }

        public async Task<ApiResult> UpdateSampleTypeAsync(int id, string sampleName, string? containerColor)
        {
            string? token = GetToken();
            return await PutApiAsync($"/api/sampletypes/{id}", new { sampleName, containerColor }, token);
        }

        public async Task<ApiResult> DeleteSampleTypeAsync(int id)
        {
            string? token = GetToken();
            return await DeleteApiAsync($"/api/sampletypes/{id}", token);
        }

        public async Task<ApiResult> ReactivateSampleTypeAsync(int id)
        {
            string? token = GetToken();
            return await PostApiAsync<object>($"/api/sampletypes/{id}/reactivate", new { }, token);
        }

        // Lab tests

        public async Task<List<LabTest>?> GetLabTestsAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<LabTest>>("/api/labtests", token);
        }

        public async Task<LabTest?> GetLabTestByIdAsync(int id)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<LabTest>($"/api/labtests/{id}", token);
        }

        public async Task<List<LabTest>?> GetInactiveLabTestsAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<LabTest>>("/api/labtests/inactive", token);
        }

        public async Task<ApiResult> CreateLabTestAsync(string testName, string? units, int sampleId)
        {
            string? token = GetToken();
            return await PostApiAsync("/api/labtests", new { testName, units, sampleId }, token);
        }

        public async Task<ApiResult> UpdateLabTestAsync(int id, string testName, string? units, int sampleId)
        {
            string? token = GetToken();
            return await PutApiAsync($"/api/labtests/{id}", new { testName, units, sampleId }, token);
        }

        public async Task<ApiResult> DeleteLabTestAsync(int id)
        {
            string? token = GetToken();
            return await DeleteApiAsync($"/api/labtests/{id}", token);
        }

        public async Task<ApiResult> ReactivateLabTestAsync(int id)
        {
            string? token = GetToken();
            return await PostApiAsync<object>($"/api/labtests/{id}/reactivate", new { }, token);
        }

        // Reference ranges

        public async Task<List<ReferenceRange>?> GetAllReferenceRangesAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<ReferenceRange>>("/api/referenceranges", token);
        }

        public async Task<ReferenceRange?> GetReferenceRangeByIdAsync(int id)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<ReferenceRange>($"/api/referenceranges/{id}", token);
        }

        public async Task<List<ReferenceRange>?> GetReferenceRangesByTestAsync(int testId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<ReferenceRange>>($"/api/referenceranges/by-test/{testId}", token);
        }

        public async Task<List<ReferenceRange>?> GetInactiveReferenceRangesAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<ReferenceRange>>("/api/referenceranges/inactive", token);
        }

        public async Task<ApiResult> CreateReferenceRangeAsync(int testId, string gender,
            int minAgeYear, int maxAgeYear, decimal minVal, decimal maxVal)
        {
            string? token = GetToken();
            return await PostApiAsync("/api/referenceranges",
                new { testId, gender, minAgeYear, maxAgeYear, minVal, maxVal }, token);
        }

        public async Task<ApiResult> UpdateReferenceRangeAsync(int id, int testId, string gender,
            int minAgeYear, int maxAgeYear, decimal minVal, decimal maxVal)
        {
            string? token = GetToken();
            return await PutApiAsync($"/api/referenceranges/{id}",
                new { testId, gender, minAgeYear, maxAgeYear, minVal, maxVal }, token);
        }

        public async Task<ApiResult> DeleteReferenceRangeAsync(int id)
        {
            string? token = GetToken();
            return await DeleteApiAsync($"/api/referenceranges/{id}", token);
        }

        public async Task<ApiResult> ReactivateReferenceRangeAsync(int id)
        {
            string? token = GetToken();
            return await PostApiAsync<object>($"/api/referenceranges/{id}/reactivate", new { }, token);
        }

        // Órdenes

        public async Task<List<Order>?> GetAllOrdersAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Order>>("/api/orders", token);
        }

        public async Task<List<Order>?> GetOrdersByDoctorAsync(int doctorId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Order>>($"/api/orders/by-doctor/{doctorId}", token);
        }

        public async Task<List<Order>?> GetOrdersByPatientAsync(int patientId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Order>>($"/api/orders/by-patient/{patientId}", token);
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<Order>($"/api/orders/{id}", token);
        }

        public async Task<List<OrderDetailDTO>?> GetOrderDetailsAsync(int orderId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<OrderDetailDTO>>($"/api/orders/{orderId}/details", token);
        }

        public async Task<OrderResultsSummary?> GetOrderSummaryAsync(int orderId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<OrderResultsSummary>($"/api/orders/{orderId}/summary", token);
        }

        public async Task<CreateOrderResponse?> CreateOrderAsync(int patientId, int doctorId)
        {
            string? token = GetToken();
            var result = await PostApiAsync("/api/orders", new { patientId, doctorId }, token);
            if (!result.Success) return null;
            return JsonConvert.DeserializeObject<CreateOrderResponse>(result.Body);
        }

        public async Task<ApiResult> DeleteOrderAsync(int id)
        {
            string? token = GetToken();
            return await DeleteApiAsync($"/api/orders/{id}", token);
        }

        public async Task<ApiResult> ChangeOrderStatusAsync(int id, string newStatus)
        {
            string? token = GetToken();
            return await PatchApiAsync($"/api/orders/{id}/status", new { newStatus }, token);
        }

        // Resultados

        public async Task<List<TestResult>?> GetResultsByOrderAsync(int orderId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<TestResult>>($"/api/orders/{orderId}/results", token);
        }

        public async Task<ApiResult> AddTestResultAsync(int orderId, int testId)
        {
            string? token = GetToken();
            return await PostApiAsync($"/api/orders/{orderId}/results",
                new { testId, resultValue = (decimal?)null, notes = (string?)null }, token);
        }

        public async Task<ApiResult> UpdateTestResultAsync(int resultId, decimal resultValue,
            string? alertLevel, string? notes)
        {
            string? token = GetToken();
            return await PutApiAsync($"/api/orders/results/{resultId}",
                new { resultValue, alertLevel, notes }, token);
        }

        public async Task<ReferenceRangeDTO?> GetReferenceRangeForResultAsync(int patientId, int testId)
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<ReferenceRangeDTO>(
                $"/api/orders/reference-range?patientId={patientId}&testId={testId}", token);
        }

        // Notificaciones

        public async Task<List<Notification>?> GetAllNotificationsAsync()
        {
            string? token = GetToken();
            if (token == null) return null;
            return await CallApiAsync<List<Notification>>("/api/notifications/all", token);
        }

        public async Task<int> GetUnreadCountAsync()
        {
            string? token = GetToken();
            if (token == null) return 0;
            var r = await CallApiAsync<UnreadCountResponse>("/api/notifications/unread-count", token);
            return r?.Count ?? 0;
        }

        public async Task<ApiResult> MarkNotificationAsReadAsync(int id)
        {
            string? token = GetToken();
            return await PostApiAsync<object>($"/api/notifications/{id}/read", new { }, token);
        }

        public async Task<ApiResult> MarkAllNotificationsAsReadAsync()
        {
            string? token = GetToken();
            return await PostApiAsync<object>("/api/notifications/read-all", new { }, token);
        }
        // Página resultados de notificaciones.
        public async Task<PagedResult<Notification>?> GetMyNotificationsPagedAsync(int page = 1, int pageSize = 10, bool? isRead = null)
        {
            string? token = GetToken();
            if (token == null) return null;

            string endpoint = $"/api/notifications?page={page}&pageSize={pageSize}";
            if (isRead.HasValue) endpoint += $"&isRead={isRead.Value}";

            return await CallApiAsync<PagedResult<Notification>>(endpoint, token);
        }

        // Portal (sin token).

        public async Task<PortalTokenInfo?> GetPortalTokenInfoAsync(Guid tokenId)
            => await CallApiAsync<PortalTokenInfo>($"/api/portal/{tokenId}");

        public async Task<PortalValidateResponse?> ValidatePortalPinAsync(Guid tokenId, string pin)
        {
            var result = await PostApiAsync($"/api/portal/{tokenId}/validate", new { pin });
            if (!result.Success) return null;
            return JsonConvert.DeserializeObject<PortalValidateResponse>(result.Body);
        }
    }

}
