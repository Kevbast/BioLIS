using BioLIS.Models.Common;
using BioLIS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// HttpContextAccessor — necesario para que ApiService lea los claims
builder.Services.AddHttpContextAccessor();

// Cache en memoria para reducir llamadas repetidas a la API
builder.Services.AddMemoryCache();

// ApiService como Transient
builder.Services.AddTransient<ApiService>();

// Servicios locales
builder.Services.AddTransient<PdfReportService>();

// Sesión
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Autenticación con cookies
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, config =>
{
    config.LoginPath = "/Auth/Login";
    config.AccessDeniedPath = "/Auth/ErrorAcceso";
    config.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(UserRoles.Admin));
    options.AddPolicy("AdminOrLab", p => p.RequireRole(UserRoles.Admin, UserRoles.Laboratorio));
    options.AddPolicy("AllRoles", p => p.RequireRole(UserRoles.Admin, UserRoles.Doctor, UserRoles.Laboratorio));
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();          // ANTES de Authentication — así la sesión está disponible
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();