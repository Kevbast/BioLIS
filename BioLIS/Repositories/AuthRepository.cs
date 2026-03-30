using BioLIS.Data;
using BioLIS.Helpers;
using BioLIS.Models;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Repositories
{
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

        public async Task<UserValidation?> ValidateCredentialsAsync(string username, string password)
        {
            // La vista V_UserValidation ya filtra por IsActive = 1 en la base de datos
            var userValidation = await this.context.Usersvalidations
                .FirstOrDefaultAsync(u => u.Username == username);

            if (userValidation == null)
                return null;

            byte[] inputHash = HelperCryptography.EncryptPassword(password, userValidation.Salt);
            bool isValid = HelperTools.CompareArrays(inputHash, userValidation.PasswordHash);

            return isValid ? userValidation : null;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await this.context.Users
                .Include(u => u.Doctor)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.IsActive);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await this.context.Users
                .Include(u => u.Doctor)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        }

        public async Task<List<User>> GetInactiveUsersAsync()
        {
            return await this.context.Users
                .Include(u => u.Doctor)
                .Include(u => u.Role)
                .Where(u => !u.IsActive)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        #endregion

        #region GESTIÓN DE USUARIOS

        public async Task<List<UserValidation>> GetAllUsersAsync()
        {
            // V_UserValidation ya filtra por activos
            return await this.context.Usersvalidations
                .OrderBy(u => u.UserID)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> ReactivateUserAsync(int userId)
        {
            var user = await this.context.Users
                .Include(u => u.Doctor)
                .FirstOrDefaultAsync(u => u.UserID == userId && !u.IsActive);

            if (user == null)
                return (false, "Usuario no encontrado o ya está activo");

            if (user.DoctorID.HasValue && (user.Doctor == null || !user.Doctor.IsActive))
                return (false, "No se puede reactivar este usuario porque su médico asociado está desactivado.");

            user.IsActive = true;
            await this.context.SaveChangesAsync();

            return (true, "Usuario reactivado exitosamente");
        }

        public async Task<List<UserValidation>> GetUsersByRoleAsync(string role)
        {
            return await this.context.Usersvalidations
                .Where(u => u.Role == role)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await this.context.Users.AnyAsync(u => u.Username == username && u.IsActive);
        }

        public async Task<bool> DoctorHasUserAsync(int doctorId)
        {
            return await this.context.Users.AnyAsync(u => u.DoctorID == doctorId && u.IsActive);
        }

        #endregion

        #region CREAR USUARIO

        public async Task<(bool Success, string Message, User? User)> CreateUserAsync(
            string username, string password, string roleName,
            string? email = null, string? photoFilename = null, int? doctorId = null)
        {
            if (string.IsNullOrWhiteSpace(username)) return (false, "El nombre de usuario es obligatorio", null);
            if (string.IsNullOrWhiteSpace(password)) return (false, "La contraseña es obligatoria", null);
            if (await UsernameExistsAsync(username)) return (false, $"El usuario '{username}' ya existe", null);

            // NUEVO: Buscar el RoleID basado en el string (RBAC)
            var roleEntity = await this.context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (roleEntity == null) return (false, $"Rol '{roleName}' no válido", null);

            if (roleName == UserRoles.Doctor && !doctorId.HasValue)
                return (false, "Los usuarios con rol 'Doctor' deben tener un Doctor asociado", null);

            if (doctorId.HasValue && await DoctorHasUserAsync(doctorId.Value))
                return (false, "Este doctor ya tiene un usuario asociado", null);

            try
            {
                int newId = await this.helper.GetNextIdAsync("Users");

                User user = new User
                {
                    UserID = newId,
                    Username = username,
                    Email = email ?? "",
                    PhotoFilename = photoFilename ?? "default-user.png",
                    PasswordText = password,
                    RoleID = roleEntity.RoleID, // Asignamos el ID, no el texto
                    DoctorID = doctorId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                string salt = HelperTools.GenerateSalt();
                byte[] passwordHash = HelperCryptography.EncryptPassword(password, salt);

                UserSecurity security = new UserSecurity
                {
                    UserID = newId,
                    Salt = salt,
                    PasswordHash = passwordHash
                };

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

        public async Task<(bool Success, string Message)> UpdateUserAsync(
            int userId, string username, string? email, string? photoFilename,
            string? newPassword, string roleName, int? doctorId)
        {
            var user = await this.context.Users
                .Include(u => u.UserSecurity)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.IsActive);

            if (user == null) return (false, "Usuario no encontrado");

            if (user.Username != username && await UsernameExistsAsync(username))
                return (false, $"El usuario '{username}' ya existe");

            if (doctorId.HasValue && user.DoctorID != doctorId && await DoctorHasUserAsync(doctorId.Value))
                return (false, "Este doctor ya tiene un usuario asociado");

            // Buscar nuevo RoleID
            var roleEntity = await this.context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (roleEntity == null) return (false, $"Rol '{roleName}' no encontrado");

            try
            {
                user.Username = username;
                user.Email = email ?? "";
                user.PhotoFilename = photoFilename ?? user.PhotoFilename;
                user.RoleID = roleEntity.RoleID; // Actualizamos el ID
                user.DoctorID = doctorId;

                if (!string.IsNullOrWhiteSpace(newPassword) && user.PasswordText != newPassword)
                {
                    user.PasswordText = newPassword;
                    if (user.UserSecurity != null)
                    {
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

        public async Task<(bool Success, string Message)> DeleteUserAsync(int userId)
        {
            var user = await this.context.Users
                .FirstOrDefaultAsync(u => u.UserID == userId && u.IsActive);

            if (user == null) return (false, "Usuario no encontrado");

            // Evitar borrar el último Admin activo usando la vista
            var adminUser = await this.context.Usersvalidations.FirstOrDefaultAsync(u => u.UserID == userId);
            if (adminUser != null && adminUser.Role == UserRoles.Admin)
            {
                int adminCount = await this.context.Usersvalidations.CountAsync(u => u.Role == UserRoles.Admin);
                if (adminCount <= 1) return (false, "No puedes eliminar al único administrador activo del sistema");
            }

            try
            {
                // SOFT DELETE: No usamos .Remove(user)
                user.IsActive = false;
                await this.context.SaveChangesAsync();
                return (true, "Usuario desactivado exitosamente");
            }
            catch (Exception ex)
            {
                return (false, $"Error al eliminar: {ex.Message}");
            }
        }

        #endregion

        #region CAMBIAR CONTRASEÑA
        public async Task<(bool Success, string Message)> ChangePasswordAsync(
            int userId, string currentPassword, string newPassword)
        {
            var user = await this.context.Users
                .Include(u => u.UserSecurity)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.IsActive);

            if (user == null || user.UserSecurity == null)
                return (false, "Usuario no encontrado");

            byte[] currentHash = HelperCryptography.EncryptPassword(currentPassword, user.UserSecurity.Salt);

            if (!HelperTools.CompareArrays(currentHash, user.UserSecurity.PasswordHash))
                return (false, "La contraseña actual es incorrecta");

            try
            {
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
        public async Task<Dictionary<string, int>> CountUsersByRoleAsync()
        {
            var result = new Dictionary<string, int>();
            foreach (var role in UserRoles.GetAll())
            {
                var count = await this.context.Usersvalidations.CountAsync(u => u.Role == role);
                result[role] = count;
            }
            return result;
        }
        #endregion
    }
}