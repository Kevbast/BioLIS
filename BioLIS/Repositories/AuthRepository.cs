using BioLab.Models;
using BioLIS.Data;
using BioLIS.Helpers;
using BioLIS.Models;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Repositories
{
    /// <summary>
    /// Repositorio de autenticación y gestión de usuarios
    /// Con cifrado seguro (Salt + SHA512)
    /// </summary>
    public class AuthRepository
    {
        private readonly LaboratorioContext context;
        private readonly HelperRepository helper;

        public AuthRepository(LaboratorioContext context, HelperRepository helper)
        {
            this.context = context;
            this.helper = helper;
        }

        #region AUTENTICACIÓN

        /// <summary>
        /// Validar credenciales usando la vista V_UserValidation
        /// </summary>
        public async Task<UserValidation?> ValidateCredentialsAsync(string username, string password)
        {
            // Buscar en la vista (más eficiente, ya tiene el JOIN)
            var userValidation = await this.context.Usersvalidations
                .FirstOrDefaultAsync(u => u.Username == username);

            if (userValidation == null)
                return null;

            // Cifrar contraseña ingresada con el Salt del usuario
            byte[] inputHash = HelperCryptography.EncryptPassword(password, userValidation.Salt);

            // Comparar hashes byte a byte
            bool isValid = HelperTools.CompareArrays(inputHash, userValidation.PasswordHash);

            return isValid ? userValidation : null;
        }

        /// <summary>
        /// Obtener usuario completo por UserID
        /// </summary>
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await this.context.Users
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.UserID == userId);
        }

        /// <summary>
        /// Obtener usuario por username
        /// </summary>
        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await this.context.Users
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        #endregion

        #region GESTIÓN DE USUARIOS

        /// <summary>
        /// Obtener todos los usuarios (desde la vista)
        /// </summary>
        public async Task<List<UserValidation>> GetAllUsersAsync()
        {
            return await this.context.Usersvalidations
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        /// <summary>
        /// Obtener usuarios por rol
        /// </summary>
        public async Task<List<UserValidation>> GetUsersByRoleAsync(string role)
        {
            return await this.context.Usersvalidations
                .Where(u => u.Role == role)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        /// <summary>
        /// Verificar si username ya existe
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await this.context.Users.AnyAsync(u => u.Username == username);
        }

        //Verificar si un médico ya tiene usuario

        public async Task<bool> DoctorHasUserAsync(int doctorId)
        {
            return await this.context.Users.AnyAsync(u => u.DoctorID == doctorId);
        }

        #endregion

        #region CREAR USUARIO

        /// <summary>
        /// Crear nuevo usuario con validaciones completas
        /// </summary>
        public async Task<(bool Success, string Message, User? User)> CreateUserAsync(
            string username, string password, string role,
            string? email = null, string? photoFilename = null, int? doctorId = null)
        {
            // VALIDACIONES
            if (string.IsNullOrWhiteSpace(username))
                return (false, "El nombre de usuario es obligatorio", null);

            if (string.IsNullOrWhiteSpace(password))
                return (false, "La contraseña es obligatoria", null);

            if (await UsernameExistsAsync(username))
                return (false, $"El usuario '{username}' ya existe", null);

            if (!UserRoles.GetAll().Contains(role))
                return (false, $"Rol '{role}' no válido", null);

            if (role == UserRoles.Doctor && !doctorId.HasValue)
                return (false, "Los usuarios con rol 'Doctor' deben tener un Doctor asociado", null);

            if (doctorId.HasValue && await DoctorHasUserAsync(doctorId.Value))
                return (false, "Este doctor ya tiene un usuario asociado", null);

            try
            {
                // 1. Generar nuevo ID
                int newId = await this.helper.GetNextIdAsync("Users");

                // 2. Crear usuario
                User user = new User
                {
                    UserID = newId,
                    Username = username,
                    Email = email ?? "",
                    PhotoFilename = photoFilename ?? "default-user.png",
                    PasswordText = password, // Guardamos visible temporalmente
                    Role = role,
                    DoctorID = doctorId
                };

                // 3. Crear seguridad con Salt único
                string salt = HelperTools.GenerateSalt();
                byte[] passwordHash = HelperCryptography.EncryptPassword(password, salt);

                UserSecurity security = new UserSecurity
                {
                    UserID = newId,
                    Salt = salt,
                    PasswordHash = passwordHash
                };

                // 4. Guardar ambos
                await this.context.Users.AddAsync(user);
                await this.context.UsersSecurity.AddAsync(security);
                await this.context.SaveChangesAsync();

                return (true, "Usuario creado exitosamente", user);
            }
            catch (Exception ex)
            {
                return (false, $"Error al crear usuario: {ex.Message}", null);
            }
        }

        #endregion

        #region ACTUALIZAR USUARIO

        /// <summary>
        /// Actualizar usuario existente
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateUserAsync(
            int userId, string username, string? email, string? photoFilename,
            string? newPassword, string role, int? doctorId)
        {
            var user = await this.context.Users
                .Include(u => u.UserSecurity)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return (false, "Usuario no encontrado");

            // Verificar si el username cambió y ya existe
            if (user.Username != username && await UsernameExistsAsync(username))
                return (false, $"El usuario '{username}' ya existe");

            // Verificar si el doctor cambió y ya tiene usuario
            if (doctorId.HasValue && user.DoctorID != doctorId &&
                await DoctorHasUserAsync(doctorId.Value))
                return (false, "Este doctor ya tiene un usuario asociado");

            try
            {
                // Actualizar datos básicos
                user.Username = username;
                user.Email = email ?? "";
                user.PhotoFilename = photoFilename ?? user.PhotoFilename;
                user.Role = role;
                user.DoctorID = doctorId;

                // Si cambió la contraseña, actualizar hash
                if (!string.IsNullOrWhiteSpace(newPassword) && user.PasswordText != newPassword)
                {
                    user.PasswordText = newPassword;

                    if (user.UserSecurity != null)
                    {
                        // Generar nuevo Salt
                        string newSalt = HelperTools.GenerateSalt();
                        byte[] newHash = HelperCryptography.EncryptPassword(newPassword, newSalt);

                        user.UserSecurity.Salt = newSalt;
                        user.UserSecurity.PasswordHash = newHash;
                    }
                }

                await this.context.SaveChangesAsync();
                return (true, "Usuario actualizado exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al actualizar: {ex.Message}");
            }
        }

        #endregion

        #region ELIMINAR USUARIO

        //Eliminar usuario (Cascade borrará Users_Security automáticamente)
        public async Task<(bool Success, string Message)> DeleteUserAsync(int userId)
        {
            var user = await this.context.Users
                .Include(u => u.UserSecurity)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return (false, "Usuario no encontrado");

            // No permitir eliminar al único admin
            if (user.Role == UserRoles.Admin)
            {
                int adminCount = await this.context.Users.CountAsync(u => u.Role == UserRoles.Admin);
                if (adminCount <= 1)
                    return (false, "No puedes eliminar al único administrador del sistema");
            }

            try
            {
                this.context.Users.Remove(user);
                await this.context.SaveChangesAsync();
                return (true, "Usuario eliminado exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al eliminar: {ex.Message}");
            }
        }

        #endregion

        #region CAMBIAR CONTRASEÑA

        /// <summary>
        /// Cambiar contraseña verificando la actual
        /// </summary>
        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId, string currentPassword, string newPassword)
        {
            var user = await this.context.Users
                .Include(u => u.UserSecurity)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null || user.UserSecurity == null)
                return (false, "Usuario no encontrado");

            // Verificar contraseña actual
            byte[] currentHash = HelperCryptography.EncryptPassword(
                currentPassword, user.UserSecurity.Salt);

            if (!HelperTools.CompareArrays(currentHash, user.UserSecurity.PasswordHash))
                return (false, "La contraseña actual es incorrecta");

            try
            {
                // Generar nuevo Salt y Hash
                string newSalt = HelperTools.GenerateSalt();
                byte[] newHash = HelperCryptography.EncryptPassword(newPassword, newSalt);

                user.PasswordText = newPassword;
                user.UserSecurity.Salt = newSalt;
                user.UserSecurity.PasswordHash = newHash;

                await this.context.SaveChangesAsync();
                return (true, "Contraseña actualizada exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al cambiar contraseña: {ex.Message}");
            }
        }

        #endregion

        #region ESTADÍSTICAS

        /// <summary>
        /// Contar usuarios por rol
        /// </summary>
        public async Task<Dictionary<string, int>> CountUsersByRoleAsync()
        {
            var result = new Dictionary<string, int>();

            foreach (var role in UserRoles.GetAll())
            {
                var count = await this.context.Users.CountAsync(u => u.Role == role);
                result[role] = count;
            }

            return result;
        }

        #endregion
    }
}