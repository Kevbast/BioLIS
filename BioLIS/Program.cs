
using BioLIS.Data;
using BioLIS.Hubs;
using BioLIS.Helpers;
using BioLIS.Models;
using BioLIS.Repositories;
using BioLIS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// AUTORIZACIÓN CON POLICIES
// ========================================
builder.Services.AddAuthorization(options =>
{
    // Solo Admin
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Admin o Laboratorio (registrar pacientes, ingresar resultados)
    options.AddPolicy("AdminOrLab", policy =>
        policy.RequireRole("Admin", "Laboratorio"));

    // Todos los roles autenticados (lectura general)
    options.AddPolicy("AllRoles", policy =>
        policy.RequireRole("Admin", "Doctor", "Laboratorio"));
});

// ========================================
// SESSION Y TEMPDATA
// ========================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ========================================
// AUTHENTICATION CON COOKIES
// ========================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, config =>
{
    config.LoginPath = "/Auth/Login";
    config.AccessDeniedPath = "/Auth/ErrorAcceso";
    config.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// ========================================
// CONTROLLERS CON TEMPDATA
// ========================================
builder.Services.AddControllersWithViews()
    .AddSessionStateTempDataProvider();

builder.Services.AddSignalR();

// ========================================
// DBCONTEXT
// ========================================
string connectionString = builder.Configuration.GetConnectionString("SqlLaboratorio");
builder.Services.AddDbContext<LaboratorioContext>(options =>
    options.UseSqlServer(connectionString));

// ========================================
// HELPERS
// ========================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<HelperPathProvider>();

// ========================================
// REPOSITORIOS
// ========================================
builder.Services.AddTransient<HelperRepository>();
builder.Services.AddTransient<CatalogRepository>();
builder.Services.AddTransient<AuthRepository>();
builder.Services.AddTransient<OrderRepository>();

// ========================================
// SERVICIOS
// ========================================
builder.Services.AddTransient<PdfReportService>();

var app = builder.Build();

// Crear usuario admin por defecto
await InitializeDefaultAdminAsync(app.Services);

// ========================================
// MIDDLEWARE PIPELINE (ORDEN CRÍTICO)
// ========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ORDEN IMPORTANTE
app.UseAuthentication();  // 1º
app.UseAuthorization();   // 2º
app.UseSession();         // 3º

// ========================================
// ROUTING
// ========================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// ========================================
// INICIALIZAR USUARIO ADMIN
// ========================================
async Task InitializeDefaultAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LaboratorioContext>();
    var helperRepo = scope.ServiceProvider.GetRequiredService<HelperRepository>();

    try
    {
        bool hasUsers = await context.Users.AnyAsync();

        if (!hasUsers)
        {
            Console.WriteLine("🔧 Creando usuario admin por defecto...");

            int newId = await helperRepo.GetNextIdAsync("Users");

            var adminUser = new User
            {
                UserID = newId,
                Username = "admin",
                Email = "admin@biolablis.com",
                PhotoFilename = "default.png",
                PasswordText = "12345",
                RoleID = 1, // <-- SOLUCIÓN: 1 es el RoleID para 'Admin'
                DoctorID = null,
                IsActive = true
            };

            string salt = HelperTools.GenerateSalt();
            byte[] passwordHash = HelperCryptography.EncryptPassword("12345", salt);

            var security = new UserSecurity
            {
                UserID = newId,
                Salt = salt,
                PasswordHash = passwordHash
            };

            await context.Users.AddAsync(adminUser);
            await context.UsersSecurity.AddAsync(security);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Usuario admin creado");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al inicializar admin: {ex.Message}");
    }
}