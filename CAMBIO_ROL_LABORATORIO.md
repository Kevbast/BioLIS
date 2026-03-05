# ? Cambios Realizados - Rol "Laboratorio"

## ?? Resumen de Cambios

Se ha actualizado el sistema para cambiar el rol **"Recepcion"** a **"Laboratorio"** manteniendo toda la seguridad con Users_Security (Salt + SHA512).

---

## ?? Archivos Modificados

### 1. **`BioLIS\Models\User.cs`**

? **Cambio en el comentario de la propiedad Role:**
```csharp
// ANTES:
public string Role { get; set; } = null!; // 'Admin', 'Doctor', 'Recepcion'

// DESPUÉS:
public string Role { get; set; } = null!; // 'Admin', 'Doctor', 'Laboratorio'
```

? **Cambio en la clase UserRoles:**
```csharp
// ANTES:
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Recepcion = "Recepcion";

    public static List<string> GetAll() => new List<string> { Admin, Doctor, Recepcion };
}

// DESPUÉS:
public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Laboratorio = "Laboratorio";

    public static List<string> GetAll() => new List<string> { Admin, Doctor, Laboratorio };
}
```

---

### 2. **`BioLIS\Repositories\AuthRepository.cs`**

? **Cambio en PhotoFilename por defecto:**
```csharp
// ANTES:
PhotoFilename = photoFilename ?? "default-user.png",

// DESPUÉS:
PhotoFilename = photoFilename ?? "default.png",
```

---

## ? Sistema de Seguridad (Mantenido sin cambios)

El sistema mantiene toda la seguridad original:

### ?? **Tabla Users_Security**
- ? Salt único de 50 caracteres por usuario
- ? Hash SHA512 con 7 iteraciones
- ? Comparación byte a byte (previene timing attacks)

### ?? **Cookies de Sesión**
```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);     // Expira en 2 horas
    options.Cookie.HttpOnly = true;                  // Protección XSS
    options.Cookie.IsEssential = true;               // No requiere consentimiento GDPR
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTP local / HTTPS producción
});
```

### ?? **Funciones Mantenidas:**
- ? `ValidateCredentialsAsync` - Usa UserValidation (vista con JOIN)
- ? `CreateUserAsync` - Crea User + UserSecurity con Salt y Hash
- ? `UpdateUserAsync` - Actualiza User + regenera Salt y Hash si cambia password
- ? `DeleteUserAsync` - Elimina User (Cascade elimina UserSecurity)
- ? `ChangePasswordAsync` - Verifica password actual con Hash, genera nuevo Salt y Hash

---

## ?? Roles Disponibles Ahora

| Rol | Constante | Descripción |
|-----|-----------|-------------|
| **Admin** | `UserRoles.Admin` | Administrador del sistema |
| **Doctor** | `UserRoles.Doctor` | Médico (requiere DoctorID) |
| **Laboratorio** | `UserRoles.Laboratorio` | Personal de laboratorio (antes "Recepcion") |

---

## ??? Base de Datos

### ?? Importante:
Si ya tienes usuarios en la base de datos con rol "Recepcion", debes actualizarlos manualmente:

```sql
UPDATE Users
SET Role = 'Laboratorio'
WHERE Role = 'Recepcion'
```

---

## ?? Uso en el Código

### **Verificar rol:**
```csharp
if (user.Role == UserRoles.Laboratorio)
{
    // Lógica para personal de laboratorio
}
```

### **Filtro de autorización:**
```csharp
[AuthorizeRole("Admin", "Laboratorio")]
public class LabTestsController : Controller
{
    // Solo Admin y Laboratorio pueden acceder
}
```

---

## ? Compilación

```
Build successful ?
```

Todo funciona correctamente.

---

## ?? Resumen de Seguridad

```
Login Process:
?
?? Usuario ingresa username + password
?
?? AuthRepository.ValidateCredentialsAsync()
?   ?? Busca en vista V_UserValidation (tiene Salt)
?   ?? Cifra password con Salt: SHA512(Salt + password + Salt) x7 iteraciones
?   ?? Compara byte a byte: inputHash == storedHash
?   ?? Si coincide ? Devuelve UserValidation
?
?? AuthController obtiene User completo
?
?? Crea sesión con: UserID, Username, Role, Photo
    ?? Cookie HttpOnly, IsEssential, SecurePolicy.SameAsRequest
```

---

## ?? ¿Qué NO cambió?

? Sistema de cifrado (Salt + SHA512)
? Tabla Users_Security
? Proceso de login con hash
? Configuración de cookies
? Filtros de autorización
? Gestión de sesiones

Solo cambió: **"Recepcion"** ? **"Laboratorio"**

---

**¡Cambios completados exitosamente! ??**

Ahora el sistema usa el rol "Laboratorio" en lugar de "Recepcion", manteniendo toda la seguridad con Users_Security.
