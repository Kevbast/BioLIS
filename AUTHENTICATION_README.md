# ?? Sistema de Autenticación BioLIS

## Credenciales por Defecto

Al iniciar la aplicación por primera vez, se crea automáticamente un usuario administrador:

- **Usuario:** `admin`
- **Contraseña:** `12345`
- **Rol:** `Admin`

## Características Implementadas

### ? Autenticación Segura
- Sistema de login con validación de credenciales
- Cifrado de contraseñas usando **SHA512 + Salt único** por usuario
- Protección contra ataques de fuerza bruta

### ? Gestión de Sesiones
- Sesiones configuradas con tiempo de expiración de **2 horas**
- Cookies seguras (HttpOnly)
- Información del usuario en sesión (UserID, Username, Role, Photo)

### ? Control de Acceso
- Filtros de autorización personalizados:
  - `[AuthorizeSession]` - Requiere usuario autenticado
  - `[AuthorizeRole("Admin", "Doctor")]` - Requiere roles específicos
- Redirección automática al login si no hay sesión activa
- Página de acceso denegado para usuarios sin permisos

### ? Interfaz de Usuario
- Vista de login moderna con diseño responsive
- Navbar con información del usuario logueado
- Dropdown con foto de perfil, nombre de usuario y rol
- Botón de cierre de sesión
- Mensajes de éxito/error con TempData

## Estructura de Archivos

```
BioLIS/
??? Controllers/
?   ??? AuthController.cs          # Login, Logout, AccessDenied
?   ??? HomeController.cs          # Con [AuthorizeSession]
?   ??? PatientsController.cs      # Con [AuthorizeSession]
??? Filters/
?   ??? AuthorizeSessionAttribute.cs  # Filtros personalizados
??? Repositories/
?   ??? AuthRepository.cs          # Lógica de autenticación
??? Views/
?   ??? Auth/
?   ?   ??? Login.cshtml           # Pantalla de login
?   ?   ??? AccessDenied.cshtml    # Acceso denegado
?   ??? Home/
?   ?   ??? Index.cshtml           # Dashboard con bienvenida
?   ??? Shared/
?       ??? _Layout.cshtml         # Navbar con info de usuario
??? Program.cs                     # Configuración de sesiones y usuario admin
```

## Uso del Sistema

### 1. Primer Inicio
```bash
# Al ejecutar la aplicación por primera vez:
# Se creará automáticamente el usuario admin
# Verás en la consola:
# ? Usuario admin creado exitosamente (ID: 1)
#   - Usuario: admin
#   - Contraseña: 12345
#   - Rol: Admin
```

### 2. Iniciar Sesión
1. Navega a `https://localhost:XXXX/Auth/Login`
2. Ingresa las credenciales:
   - Usuario: `admin`
   - Contraseña: `12345`
3. Serás redirigido al Dashboard

### 3. Proteger Controladores
Para requerir autenticación en un controlador:

```csharp
[AuthorizeSession]
public class MiController : Controller
{
    // Todas las acciones requieren autenticación
}
```

Para requerir roles específicos:

```csharp
[AuthorizeRole("Admin", "Doctor")]
public class AdminController : Controller
{
    // Solo Admin y Doctor pueden acceder
}
```

### 4. Acceder a Datos de Sesión en Vistas
```razor
@if (Context.Session.GetInt32("UserID").HasValue)
{
    <p>Usuario: @Context.Session.GetString("Username")</p>
    <p>Rol: @Context.Session.GetString("Role")</p>
}
```

### 5. Crear Nuevos Usuarios
Usa el `AuthRepository`:

```csharp
var result = await authRepo.CreateUserAsync(
    username: "doctor1",
    password: "miPassword123",
    role: UserRoles.Doctor,
    email: "doctor1@biolablis.com",
    photoFilename: "doctor1.jpg",
    doctorId: 1
);

if (result.Success)
{
    Console.WriteLine($"Usuario creado: {result.User.Username}");
}
```

## Roles Disponibles

```csharp
UserRoles.Admin       // Administrador del sistema
UserRoles.Doctor      // Médico (debe tener DoctorID asociado)
UserRoles.Recepcion   // Personal de recepción
```

## Seguridad

### Cifrado de Contraseñas
- Cada usuario tiene un **Salt único** de 50 caracteres aleatorios
- Las contraseñas se cifran usando **SHA512** con 7 iteraciones
- Los hashes se comparan byte a byte para evitar timing attacks

### Protección de Datos
- Las contraseñas nunca se almacenan en texto plano (excepto `PasswordText` para fines administrativos)
- Los hashes se guardan como `VARBINARY` en la tabla `Users_Security`
- Las sesiones usan cookies HttpOnly para prevenir XSS

## Mejoras Futuras

- [ ] Implementar recuperación de contraseña
- [ ] Agregar autenticación de dos factores (2FA)
- [ ] Implementar bloqueo de cuenta tras múltiples intentos fallidos
- [ ] Agregar logs de auditoría de login/logout
- [ ] Implementar tokens JWT para APIs

## Problemas Comunes

### No puedo iniciar sesión
1. Verifica que la base de datos esté corriendo
2. Asegúrate de que el usuario `admin` fue creado (revisa la consola)
3. Intenta con las credenciales: `admin` / `12345`

### Se cierra la sesión automáticamente
- La sesión expira después de 2 horas de inactividad
- Puedes cambiar el tiempo en `Program.cs`:
  ```csharp
  options.IdleTimeout = TimeSpan.FromHours(8); // 8 horas
  ```

### No puedo acceder a una página
- Verifica que estés autenticado
- Comprueba que tu rol tiene permisos para acceder a esa página

## Soporte

Para reportar problemas o sugerencias, contacta al equipo de desarrollo.

---

**BioLIS** - Sistema de Información de Laboratorio
