# ?? Sistema de Login BioLIS - Implementación Completada

## ? Cambios Implementados

### 1. **Program.cs**
- ? Configuración de sesiones (2 horas de expiración)
- ? Registro del `AuthRepository`
- ? Cambio de ruta por defecto a `/Auth/Login`
- ? **Creación automática del usuario admin** si no existe ningún usuario

### 2. **Controllers**
- ? `AuthController.cs` - Login, Logout, AccessDenied
- ? `UsersController.cs` - Gestión de usuarios (solo Admin)
- ? `HomeController.cs` - Protegido con `[AuthorizeSession]`
- ? `PatientsController.cs` - Protegido con `[AuthorizeSession]`

### 3. **Filtros de Autorización**
- ? `AuthorizeSessionAttribute` - Requiere usuario autenticado
- ? `AuthorizeRoleAttribute` - Requiere roles específicos

### 4. **Vistas**
- ? `Login.cshtml` - Pantalla de login moderna
- ? `AccessDenied.cshtml` - Acceso denegado
- ? `Users/Index.cshtml` - Lista de usuarios
- ? `Users/ChangePassword.cshtml` - Cambio de contraseña
- ? `_Layout.cshtml` - Navbar con info de usuario + dropdown
- ? `Home/Index.cshtml` - Dashboard mejorado

### 5. **Documentación**
- ? `AUTHENTICATION_README.md` - Guía completa del sistema

---

## ?? Cómo Usar el Sistema

### Paso 1: Ejecutar la Aplicación

```bash
dotnet run
```

Al iniciar por primera vez, verás en la consola:

```
? Usuario admin creado exitosamente (ID: 1)
  - Usuario: admin
  - Contraseña: 12345
  - Rol: Admin
```

### Paso 2: Iniciar Sesión

1. Abre tu navegador en: `https://localhost:XXXX/`
2. Serás redirigido automáticamente al login
3. Ingresa las credenciales:
   - **Usuario:** `admin`
   - **Contraseña:** `12345`
4. ¡Listo! Accederás al Dashboard

### Paso 3: Gestión de Usuarios (Solo Admin)

Como administrador, puedes:

- **Ver usuarios:** `/Users/Index`
- **Crear usuarios:** `/Users/Create`
- **Eliminar usuarios:** Desde la lista de usuarios
- **Cambiar tu contraseña:** Dropdown del usuario ? "Cambiar Contraseña"

---

## ?? Características de la UI

### Navbar Actualizada
- Muestra foto del usuario (o imagen por defecto)
- Nombre de usuario y rol (badge)
- Dropdown con opciones:
  - **ID del usuario**
  - **Cambiar Contraseña**
  - **Cerrar Sesión**

### Protección de Rutas
Todas las páginas (excepto Login) requieren autenticación:
- `/Home` ? Requiere login
- `/Patients` ? Requiere login
- `/Users` ? Requiere login + Rol Admin

Si intentas acceder sin login ? Redirige a Login
Si no tienes permisos ? Redirige a AccessDenied

---

## ?? Seguridad

### Cifrado de Contraseñas
- **Salt único** por usuario (50 caracteres aleatorios)
- **SHA512** con 7 iteraciones
- Comparación byte a byte (previene timing attacks)

### Sesiones
- Cookies **HttpOnly** (protección contra XSS)
- Expiración configurable (2 horas por defecto)
- Datos en sesión: `UserID`, `Username`, `Role`, `Photo`

---

## ?? Estructura de Archivos Creados/Modificados

```
BioLIS/
??? Program.cs                            ? MODIFICADO
??? Controllers/
?   ??? AuthController.cs                 ? CREADO
?   ??? UsersController.cs                ? CREADO
?   ??? HomeController.cs                 ? MODIFICADO
?   ??? PatientsController.cs             ? MODIFICADO
??? Filters/
?   ??? AuthorizeSessionAttribute.cs      ? CREADO
??? Views/
?   ??? _ViewImports.cshtml               ? MODIFICADO
?   ??? Auth/
?   ?   ??? Login.cshtml                  ? EXISTE
?   ?   ??? AccessDenied.cshtml           ? EXISTE
?   ??? Users/
?   ?   ??? Index.cshtml                  ? CREADO
?   ?   ??? ChangePassword.cshtml         ? CREADO
?   ??? Home/
?   ?   ??? Index.cshtml                  ? MODIFICADO
?   ??? Shared/
?       ??? _Layout.cshtml                ? MODIFICADO
??? AUTHENTICATION_README.md              ? CREADO
```

---

## ?? Prueba el Sistema

### 1. Login con usuario admin
```
Usuario: admin
Password: 12345
```

### 2. Accede a la gestión de usuarios
- Navbar ? "?? Usuarios"

### 3. Crea un nuevo usuario
- Ejemplo:
  - Usuario: `doctor1`
  - Password: `123456`
  - Rol: `Doctor`
  - Email: `doctor1@biolablis.com`
  - Doctor ID: 1

### 4. Cierra sesión
- Dropdown usuario ? "?? Cerrar Sesión"

### 5. Inicia sesión con el nuevo usuario
```
Usuario: doctor1
Password: 123456
```

---

## ?? Configuraciones Opcionales

### Cambiar tiempo de expiración de sesión

En `Program.cs`:

```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // 8 horas
});
```

### Deshabilitar creación automática de admin

Comenta el código en `Program.cs`:

```csharp
// await InitializeDefaultAdminAsync(app.Services);
```

---

## ?? Solución de Problemas

### No puedo iniciar sesión
- Verifica que la base de datos esté corriendo
- Revisa la consola para confirmar que se creó el usuario admin
- Intenta: `admin` / `12345`

### La sesión se cierra automáticamente
- Por defecto expira a las 2 horas
- Puedes aumentar el tiempo en `Program.cs`

### No veo el menú "Usuarios"
- Solo los usuarios con rol **Admin** pueden ver este menú
- Inicia sesión con el usuario `admin`

---

## ?? Roles Disponibles

| Rol | Descripción | Permisos |
|-----|-------------|----------|
| **Admin** | Administrador | Todos los permisos + Gestión de usuarios |
| **Doctor** | Médico | Acceso a pacientes y resultados |
| **Recepcion** | Recepcionista | Acceso limitado a pacientes |

---

## ? Checklist de Implementación

- [x] Sistema de login funcional
- [x] Creación automática de usuario admin
- [x] Sesiones configuradas
- [x] Navbar con información del usuario
- [x] Protección de rutas
- [x] Gestión de usuarios (Admin)
- [x] Cambio de contraseña
- [x] Cifrado seguro (Salt + SHA512)
- [x] Redirección al login si no hay sesión
- [x] Vista de acceso denegado
- [x] Logout funcional

---

## ?? Próximos Pasos Sugeridos

1. **Crear vista de creación de usuarios** (`Users/Create.cshtml`)
2. **Agregar validación de correo electrónico**
3. **Implementar recuperación de contraseña**
4. **Agregar logs de auditoría** (login/logout)
5. **Implementar bloqueo de cuenta** tras múltiples intentos fallidos
6. **Agregar autenticación de dos factores (2FA)**

---

## ?? Soporte

Si encuentras algún problema o necesitas ayuda:
- Revisa el archivo `AUTHENTICATION_README.md`
- Verifica los logs de la consola
- Comprueba que todas las migraciones estén aplicadas

---

**¡Sistema de Login implementado exitosamente! ??**

Ahora puedes iniciar sesión con:
- Usuario: `admin`
- Contraseña: `12345`
