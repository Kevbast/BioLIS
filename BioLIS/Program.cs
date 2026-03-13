using BioLab.Models;
using BioLIS.Data;
using BioLIS.Helpers;
using BioLIS.Models;
using BioLIS.Repositories;
using BioLIS.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//CONEXION SQLSERVER
string connectionString = builder.Configuration.GetConnectionString("SqlLaboratorio");

builder.Services.AddDbContext<LaboratorioContext>(options =>
    options.UseSqlServer(connectionString));

// Configuración de sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // Tiempo de expiración de sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Helpers
builder.Services.AddHttpContextAccessor(); // Requerido para HelperPathProvider
builder.Services.AddTransient<HelperPathProvider>();

// Repositorios
builder.Services.AddTransient<HelperRepository>();
builder.Services.AddTransient<CatalogRepository>();
builder.Services.AddTransient<AuthRepository>();
builder.Services.AddTransient<OrderRepository>();

// Servicios
builder.Services.AddTransient<PdfReportService>();

var app = builder.Build();

// Crear usuario admin por defecto si no existe ningún usuario
await InitializeDefaultAdminAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession(); // Activar sesiones antes de autorización
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}") // Ruta por defecto al Login
    .WithStaticAssets();


app.Run();

// Método para inicializar el usuario admin por defecto
async Task InitializeDefaultAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LaboratorioContext>();
    var helperRepo = scope.ServiceProvider.GetRequiredService<HelperRepository>();
    
    try
    {
        // Verificar si hay usuarios en la base de datos
        bool hasUsers = await context.Users.AnyAsync();
        
        if (!hasUsers)
        {
            Console.WriteLine("No se encontraron usuarios. Creando usuario admin por defecto...");
            
            // Generar ID para el nuevo usuario
            int newId = await helperRepo.GetNextIdAsync("Users");
            
            // Crear usuario admin
            var adminUser = new User
            {
                UserID = newId,
                Username = "admin",
                Email = "admin@biolablis.com",
                PhotoFilename = "default.png",
                PasswordText = "12345",
                Role = UserRoles.Admin,
                DoctorID = null
            };
            
            // Crear seguridad con Salt y Hash
            string salt = HelperTools.GenerateSalt();
            byte[] passwordHash = HelperCryptography.EncryptPassword("12345", salt);
            
            var security = new UserSecurity
            {
                UserID = newId,
                Salt = salt,
                PasswordHash = passwordHash
            };
            
            // Guardar en la base de datos
            await context.Users.AddAsync(adminUser);
            await context.UsersSecurity.AddAsync(security);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"✓ Usuario admin creado exitosamente (ID: {newId})");
            Console.WriteLine("  - Usuario: admin");
            Console.WriteLine("  - Contraseña: 12345");
            Console.WriteLine("  - Rol: Admin");
        }
        else
        {
            Console.WriteLine("Ya existen usuarios en la base de datos.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al inicializar usuario admin: {ex.Message}");
    }
}
