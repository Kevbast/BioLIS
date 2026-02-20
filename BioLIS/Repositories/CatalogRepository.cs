using BioLab.Models; // O BioLIS.Models
using BioLIS.Data;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Repositories
{
    public class CatalogRepository
    {
        private LaboratorioContext context;
        private HelperRepository helper;

        //Context y Helper
        public CatalogRepository(LaboratorioContext context, HelperRepository helper)
        {
            this.context = context;
            this.helper = helper;
        }

#region PACIENTES

        public async Task CreatePatientAsync(string nombre, string apellido,
                                             string genero, DateTime fechaNac,
                                             string email, string foto)
        {
            //ID nuevo generado
            int newId = await helper.GetNextIdAsync("Patients");

            Patient paciente = new Patient
            {
                PatientID = newId,
                FirstName = nombre,
                LastName = apellido,
                Gender = genero,
                BirthDate = fechaNac,
                Email = email,
                PhotoFilename = foto
            };

            this.context.Patients.Add(paciente);
            await this.context.SaveChangesAsync();
        }

        public async Task<List<Patient>> GetPatientsAsync()
        {
            return await this.context.Patients.ToListAsync();
        }

        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            return await this.context.Patients.FindAsync(id);
        }

        // 4. ACTUALIZAR PACIENTE 
        public async Task<bool> UpdateAsync(Patient patient)
        {
            Patient paciente = await this.context.Patients.FindAsync(patient.PatientID);
            if (paciente == null)
                return false;

            paciente.FirstName = patient.FirstName;
            paciente.LastName = patient.LastName;
            paciente.Gender = patient.Gender;
            paciente.BirthDate = patient.BirthDate;
            paciente.Email = patient.Email;
            paciente.PhotoFilename = patient.PhotoFilename;

            await this.context.SaveChangesAsync();
            return true;
        }
        // 5. ELIMINAR (Delete)
        public async Task<(bool Success, string Message)> DeletePatientAsync(int id)
        {
            //Verifico si se puede borrar
            var validation = await this.helper.CanDeleteAsync("Patients", id);

            if (!validation.CanDelete)
                return (false, validation.Message);

            var patient = await this.context.Patients.FindAsync(id);
            if (patient == null)
                return (false, "Paciente no encontrado.");

            this.context.Patients.Remove(patient);
            await this.context.SaveChangesAsync();

            return (true, "Paciente eliminado exitosamente.");
        }

        // 6.Buscar pacientes por nombre o apellido
        public async Task<List<Patient>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetPatientsAsync();

            searchTerm = searchTerm.ToLower();

            return await this.context.Patients
                .Where(p => p.FirstName.ToLower().Contains(searchTerm) ||
                           p.LastName.ToLower().Contains(searchTerm))
                .OrderBy(p => p.LastName)
                .ToListAsync();
        }


        #endregion

        #region DOCTORES 
        //1.Obtener doctores
        public async Task<List<Doctor>> GetDoctorsAsync()
        {
            return await this.context.Doctors.OrderBy(d => d.FullName).ToListAsync();
        }
        
        //2.Obtener doctor por ID
        public async Task<Doctor?> GetDoctorByIdAsync(int id)
        {
            return await this.context.Doctors.FindAsync(id);
        }
        //3.Insertar Doctor
        public async Task CreateDoctorAsync(string fullName, string license, string email)
        {
            int newId = await helper.GetNextIdAsync("Doctors");

            Doctor doctor = new Doctor
            {
                DoctorID = newId,
                FullName = fullName,
                LicenseNumber = license,
                Email = email
            };

            this.context.Doctors.Add(doctor);
            await this.context.SaveChangesAsync();
        }

        //4.Actualizar Doctor
        public async Task<bool> UpdateDoctorAsync(Doctor doctor)
        {
            var existing = await this.context.Doctors.FindAsync(doctor.DoctorID);
            if (existing == null)
                return false;

            existing.FullName = doctor.FullName;
            existing.LicenseNumber = doctor.LicenseNumber;
            existing.Email = doctor.Email;

            await this.context.SaveChangesAsync();
            return true;
        }


        #endregion

        #region TIPOS DE MUESTRA Y EXÁMENES (Para los combos/selects)

        public async Task<List<SampleType>> GetSampleTypesAsync()
        {
            return await this.context.SampleTypes.ToListAsync();
        }

        public async Task<List<LabTest>> GetLabTestsAsync()
        {
            // Usamos Include para traer también el color del tubo asociado
            return await this.context.LabTests
                                     .Include(t => t.SampleType)
                                     .ToListAsync();
        }

        #endregion
    }
}