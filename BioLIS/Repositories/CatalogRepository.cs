using BioLIS.Models;
using BioLIS.Data;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Repositories
{
    public class CatalogRepository
    {
        private LaboratorioContext context;
        private HelperRepository helper;

        public LaboratorioContext Context => context;

        public CatalogRepository(LaboratorioContext context, HelperRepository helper)
        {
            this.context = context;
            this.helper = helper;
        }

        #region PACIENTES

        public async Task<List<Patient>> GetPatientsAsync()
        {
            return await this.context.Patients.Where(p => p.IsActive).ToListAsync();
        }

        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            return await this.context.Patients.FirstOrDefaultAsync(p => p.PatientID == id && p.IsActive);
        }

        // Añadido phoneNumber y currentUserId para auditoría
        public async Task CreatePatientAsync(string nombre, string apellido,
                                             string genero, DateTime fechaNac,
                                             string email, string foto, string? phoneNumber = null, int? currentUserId = null)
        {
            int newId = await helper.GetNextIdAsync("Patients");

            Patient paciente = new Patient
            {
                PatientID = newId,
                PublicId = Guid.NewGuid(),
                FirstName = nombre,
                LastName = apellido,
                Gender = genero,
                BirthDate = fechaNac,
                Email = email,
                PhoneNumber = phoneNumber,
                PhotoFilename = foto,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUserId
            };

            this.context.Patients.Add(paciente);
            await this.context.SaveChangesAsync();
        }

        public async Task<bool> UpdatePatientAsync(int patientId, string nombre, string apellido,
                                             string genero, DateTime fechaNac,
                                             string email, string foto, string? phoneNumber = null, int? currentUserId = null)
        {
            Patient paciente = await this.context.Patients.FirstOrDefaultAsync(p => p.PatientID == patientId && p.IsActive);
            if (paciente == null) return false;

            paciente.FirstName = nombre;
            paciente.LastName = apellido;
            paciente.Gender = genero;
            paciente.BirthDate = fechaNac;
            paciente.Email = email;
            paciente.PhoneNumber = phoneNumber;
            paciente.PhotoFilename = foto;
            paciente.UpdatedAt = DateTime.Now;
            paciente.UpdatedBy = currentUserId; // Auditoría

            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> DeletePatientAsync(int id, int? currentUserId = null)
        {
            var validation = await this.helper.CanDeleteAsync("Patients", id);
            if (!validation.CanDelete) return (false, validation.Message);

            var patient = await this.context.Patients.FindAsync(id);
            if (patient == null) return (false, "Paciente no encontrado.");

            // SOFT DELETE
            patient.IsActive = false;
            patient.UpdatedAt = DateTime.Now;
            patient.UpdatedBy = currentUserId;

            await this.context.SaveChangesAsync();
            return (true, "Paciente desactivado exitosamente.");
        }

        public async Task<List<Patient>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return await GetPatientsAsync();

            searchTerm = searchTerm.ToLower();

            return await this.context.Patients
                .Where(p => p.IsActive && (p.FirstName.ToLower().Contains(searchTerm) || p.LastName.ToLower().Contains(searchTerm)))
                .OrderBy(p => p.LastName)
                .ToListAsync();
        }

        public async Task<List<Patient>> GetInactivePatientsAsync()
        {
            return await this.context.Patients
                .Where(p => !p.IsActive)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> ReactivatePatientAsync(int id, int? currentUserId = null)
        {
            var patient = await this.context.Patients
                .FirstOrDefaultAsync(p => p.PatientID == id && !p.IsActive);

            if (patient == null)
                return (false, "Paciente no encontrado o ya está activo.");

            patient.IsActive = true;
            patient.UpdatedAt = DateTime.Now;
            patient.UpdatedBy = currentUserId;

            await this.context.SaveChangesAsync();
            return (true, "Paciente reactivado exitosamente.");
        }

        #endregion

        #region DOCTORES 

        public async Task<List<Doctor>> GetDoctorsAsync()
        {
            return await this.context.Doctors.Where(d => d.IsActive).OrderBy(d => d.DoctorID).ToListAsync();
        }

        public async Task<Doctor?> GetDoctorByIdAsync(int id)
        {
            return await this.context.Doctors.FirstOrDefaultAsync(d => d.DoctorID == id && d.IsActive);
        }

        public async Task CreateDoctorAsync(string fullName, string license, string email, string? phoneNumber = null, int? currentUserId = null)
        {
            int newId = await helper.GetNextIdAsync("Doctors");

            Doctor doctor = new Doctor
            {
                DoctorID = newId,
                FullName = fullName,
                LicenseNumber = license,
                Email = email,
                PhoneNumber = phoneNumber,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = currentUserId
            };

            this.context.Doctors.Add(doctor);
            await this.context.SaveChangesAsync();
        }

        public async Task<bool> UpdateDoctorAsync(Doctor doctor, int? currentUserId = null)
        {
            var existing = await this.context.Doctors.FirstOrDefaultAsync(d => d.DoctorID == doctor.DoctorID && d.IsActive);
            if (existing == null) return false;

            existing.FullName = doctor.FullName;
            existing.LicenseNumber = doctor.LicenseNumber;
            existing.Email = doctor.Email;
            existing.PhoneNumber = doctor.PhoneNumber;
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = currentUserId;

            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> DeleteDoctorAsync(int id, int? currentUserId = null)
        {
            var validation = await this.helper.CanDeleteAsync("Doctors", id);
            if (!validation.CanDelete) return (false, validation.Message);

            var doctor = await this.context.Doctors.FindAsync(id);
            if (doctor == null) return (false, "Doctor no encontrado.");

            // SOFT DELETE
            doctor.IsActive = false;
            doctor.UpdatedAt = DateTime.Now;
            doctor.UpdatedBy = currentUserId;

            var relatedUsers = await this.context.Users
                .Where(u => u.DoctorID == id && u.IsActive)
                .ToListAsync();

            foreach (var user in relatedUsers)
            {
                user.IsActive = false;
            }

            await this.context.SaveChangesAsync();
            return (true, relatedUsers.Any()
                ? $"Doctor desactivado exitosamente. También se desactivó(aron) {relatedUsers.Count} usuario(s) vinculado(s)."
                : "Doctor desactivado exitosamente.");
        }

        public async Task<List<Doctor>> GetInactiveDoctorsAsync()
        {
            return await this.context.Doctors
                .Where(d => !d.IsActive)
                .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> ReactivateDoctorAsync(int doctorId, int? currentUserId = null)
        {
            var doctor = await this.context.Doctors
                .FirstOrDefaultAsync(d => d.DoctorID == doctorId && !d.IsActive);

            if (doctor == null)
                return (false, "Médico no encontrado o ya está activo.");

            doctor.IsActive = true;
            doctor.UpdatedAt = DateTime.Now;
            doctor.UpdatedBy = currentUserId;

            await this.context.SaveChangesAsync();
            return (true, "Médico reactivado exitosamente.");
        }

        #endregion

        #region TIPOS DE MUESTRA (SAMPLE TYPES)

        public async Task<List<SampleType>> GetSampleTypesAsync()
        {
            return await this.context.SampleTypes.Where(st => st.IsActive).OrderBy(st => st.SampleID).ToListAsync();
        }

        public async Task<SampleType?> GetSampleTypeByIdAsync(int id)
        {
            return await this.context.SampleTypes
                .Include(st => st.LabTests.Where(lt => lt.IsActive))
                .FirstOrDefaultAsync(st => st.SampleID == id && st.IsActive);
        }

        public async Task<SampleType> CreateSampleTypeAsync(string sampleName, string? containerColor = null)
        {
            int newId = await helper.GetNextIdAsync("SampleTypes");

            SampleType sampleType = new SampleType
            {
                SampleID = newId,
                SampleName = sampleName,
                ContainerColor = containerColor,
                IsActive = true
            };

            this.context.SampleTypes.Add(sampleType);
            await this.context.SaveChangesAsync();
            return sampleType;
        }

        public async Task<bool> UpdateSampleTypeAsync(SampleType sampleType)
        {
            var existing = await this.context.SampleTypes.FirstOrDefaultAsync(s => s.SampleID == sampleType.SampleID && s.IsActive);
            if (existing == null) return false;

            existing.SampleName = sampleType.SampleName;
            existing.ContainerColor = sampleType.ContainerColor;

            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> DeleteSampleTypeAsync(int id)
        {
            var validation = await this.helper.CanDeleteAsync("SampleTypes", id);
            if (!validation.CanDelete) return (false, validation.Message);

            var sampleType = await this.context.SampleTypes.FindAsync(id);
            if (sampleType == null) return (false, "Tipo de muestra no encontrado.");

            sampleType.IsActive = false; // SOFT DELETE
            await this.context.SaveChangesAsync();

            return (true, "Tipo de muestra eliminado exitosamente.");
        }

        #endregion

        #region LAB TESTS

        public async Task<List<LabTest>> GetLabTestsAsync()
        {
            return await this.context.LabTests
                                     .Include(t => t.SampleType)
                                     .Where(t => t.IsActive)
                                     .OrderBy(t => t.TestID)
                                     .ToListAsync();
        }

        public async Task<LabTest?> GetLabTestByIdAsync(int id)
        {
            return await this.context.LabTests
                .Include(t => t.SampleType)
                .Include(t => t.ReferenceRanges.Where(rr => rr.IsActive))
                .FirstOrDefaultAsync(t => t.TestID == id && t.IsActive);
        }

        public async Task<LabTest> CreateLabTestAsync(string testName, string? units, int sampleId)
        {
            int newId = await helper.GetNextIdAsync("LabTests");
            LabTest labTest = new LabTest
            {
                TestID = newId,
                TestName = testName,
                Units = units,
                SampleID = sampleId,
                IsActive = true
            };

            this.context.LabTests.Add(labTest);
            await this.context.SaveChangesAsync();
            return labTest;
        }

        public async Task<bool> UpdateLabTestAsync(LabTest labTest)
        {
            var existing = await this.context.LabTests.FirstOrDefaultAsync(l => l.TestID == labTest.TestID && l.IsActive);
            if (existing == null) return false;

            existing.TestName = labTest.TestName;
            existing.Units = labTest.Units;
            existing.SampleID = labTest.SampleID;

            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> DeleteLabTestAsync(int id)
        {
            var validation = await this.helper.CanDeleteAsync("LabTests", id);
            if (!validation.CanDelete) return (false, validation.Message);

            var labTest = await this.context.LabTests.FindAsync(id);
            if (labTest == null) return (false, "Examen no encontrado.");

            labTest.IsActive = false; // SOFT DELETE
            await this.context.SaveChangesAsync();

            return (true, "Examen eliminado exitosamente.");
        }

        public async Task<List<LabTest>> GetLabTestsBySampleTypeAsync(int sampleId)
        {
            return await this.context.LabTests
                .Where(t => t.SampleID == sampleId && t.IsActive)
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

        #endregion

        #region RANGOS DE REFERENCIA

        public async Task<List<ReferenceRange>> GetReferenceRangesAsync()
        {
            return await this.context.ReferenceRanges
                .Include(rr => rr.LabTest)
                .Where(rr => rr.IsActive && rr.LabTest.IsActive)
                .OrderBy(rr => rr.LabTest.TestName)
                .ThenBy(rr => rr.Gender)
                .ThenBy(rr => rr.MinAgeYear)
                .ToListAsync();
        }

        public async Task<List<ReferenceRange>> GetAllReferenceRangesAsync()
        {
            return await GetReferenceRangesAsync();
        }

        public async Task<ReferenceRange?> GetReferenceRangeByIdAsync(int id)
        {
            return await this.context.ReferenceRanges
                .Include(rr => rr.LabTest)
                .FirstOrDefaultAsync(rr => rr.RangeID == id && rr.IsActive);
        }

        public async Task<List<ReferenceRange>> GetReferenceRangesByTestAsync(int testId)
        {
            return await this.context.ReferenceRanges
                .Where(rr => rr.TestID == testId && rr.IsActive)
                .OrderBy(rr => rr.Gender)
                .ThenBy(rr => rr.MinAgeYear)
                .ToListAsync();
        }

        public async Task<ReferenceRange> CreateReferenceRangeAsync(
            int testId, string gender, int minAge, int maxAge, decimal minVal, decimal maxVal)
        {
            int newId = await helper.GetNextIdAsync("ReferenceRanges");
            ReferenceRange referenceRange = new ReferenceRange
            {
                RangeID = newId,
                TestID = testId,
                Gender = gender,
                MinAgeYear = minAge,
                MaxAgeYear = maxAge,
                MinVal = minVal,
                MaxVal = maxVal,
                IsActive = true
            };

            this.context.ReferenceRanges.Add(referenceRange);
            await this.context.SaveChangesAsync();
            return referenceRange;
        }

        public async Task<bool> UpdateReferenceRangeAsync(ReferenceRange referenceRange)
        {
            var existing = await this.context.ReferenceRanges.FirstOrDefaultAsync(r => r.RangeID == referenceRange.RangeID && r.IsActive);
            if (existing == null) return false;

            existing.TestID = referenceRange.TestID;
            existing.Gender = referenceRange.Gender;
            existing.MinAgeYear = referenceRange.MinAgeYear;
            existing.MaxAgeYear = referenceRange.MaxAgeYear;
            existing.MinVal = referenceRange.MinVal;
            existing.MaxVal = referenceRange.MaxVal;

            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> DeleteReferenceRangeAsync(int id)
        {
            var referenceRange = await this.context.ReferenceRanges.FindAsync(id);
            if (referenceRange == null) return (false, "Rango de referencia no encontrado.");

            referenceRange.IsActive = false; // SOFT DELETE
            await this.context.SaveChangesAsync();

            return (true, "Rango de referencia eliminado exitosamente.");
        }
        #endregion

        #region NOTIFICATIONS
        public async Task CreateNotificationAsync(int userId, string title, string message, int? orderId = null)
        {
            var notification = new Notification
            {
                UserID = userId,
                Title = title,
                Message = message,
                RelatedOrderID = orderId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            this.context.Notifications.Add(notification);
            await this.context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await this.context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20) // Limitamos a las últimas 20
                .ToListAsync();
        }

        public async Task<List<Notification>> GetAllNotificationsAsync(int take = 200)
        {
            return await this.context.Notifications
                .Include(n => n.User)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetLatestUnreadNotificationsAsync(int userId, int take = 5)
        {
            return await this.context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await this.context.Notifications
                .CountAsync(n => n.UserID == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notif = await this.context.Notifications.FindAsync(notificationId);
            if (notif != null)
            {
                notif.IsRead = true;
                await this.context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadByUserAsync(int userId)
        {
            var notRead = await this.context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            if (!notRead.Any())
                return;

            foreach (var notification in notRead)
            {
                notification.IsRead = true;
            }

            await this.context.SaveChangesAsync();
        }
        #endregion

        #region DOCTORES SIN USUARIO
        public async Task<List<Doctor>> GetDoctorsWithoutUserAsync()
        {
            var doctorsWithUser = await this.context.Users
                .Where(u => u.DoctorID != null && u.IsActive)
                .Select(u => u.DoctorID.Value)
                .ToListAsync();

            return await this.context.Doctors
                .Where(d => !doctorsWithUser.Contains(d.DoctorID) && d.IsActive)
                .OrderBy(d => d.FullName)
                .ToListAsync();
        }
        #endregion
    }
}