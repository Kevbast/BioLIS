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
        //1.Obtener todos los pacientes
        public async Task<List<Patient>> GetPatientsAsync()
        {
            return await this.context.Patients.ToListAsync();
        }
        //2.Obtener paciente por ID
        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            return await this.context.Patients.FindAsync(id);
        }
        //3.Nuevo paciente(revisar si hay que cambiarlo más adelante)------
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
        //5.Eliminar Doctor
        public async Task<(bool Success, string Message)> DeleteDoctorAsync(int id)
        {
            var validation = await this.helper.CanDeleteAsync("Doctors", id);

            if (!validation.CanDelete)
                return (false, validation.Message);

            var doctor = await this.context.Doctors.FindAsync(id);
            if (doctor == null)
                return (false, "Doctor no encontrado.");

            this.context.Doctors.Remove(doctor);
            await this.context.SaveChangesAsync();

            return (true, "Doctor eliminado exitosamente.");
        }


#endregion

#region TIPOS DE MUESTRA (SAMPLE TYPES)
        // 1.Obtener todas los tipos de muestra 
        public async Task<List<SampleType>> GetSampleTypesAsync()
        {
            return await this.context.SampleTypes
                .OrderBy(st => st.SampleName)
                .ToListAsync();
        }
        //2.Obtener tipo de muestra por ID
        public async Task<SampleType?> GetSampleTypeByIdAsync(int id)
        {
            return await this.context.SampleTypes
                .Include(st => st.LabTests)
                .FirstOrDefaultAsync(st => st.SampleID == id);
        }
        //3.Crear tipo de muestra
        public async Task<SampleType> CreateSampleTypeAsync(string sampleName, string? containerColor = null)
        {
            int newId = await helper.GetNextIdAsync("SampleTypes");

            SampleType sampleType = new SampleType
            {
                SampleID = newId,
                SampleName = sampleName,
                ContainerColor = containerColor
            };

            this.context.SampleTypes.Add(sampleType);
            await this.context.SaveChangesAsync();

            return sampleType;
        }
        //4.Actualizar tipo de muestra
        public async Task<bool> UpdateSampleTypeAsync(SampleType sampleType)
        {
            var existing = await this.context.SampleTypes.FindAsync(sampleType.SampleID);
            if (existing == null)
                return false;

            existing.SampleName = sampleType.SampleName;
            existing.ContainerColor = sampleType.ContainerColor;

            await this.context.SaveChangesAsync();
            return true;
        }
        //5.Eliminar tipo de muestra
        public async Task<(bool Success, string Message)> DeleteSampleTypeAsync(int id)
        {
            var validation = await this.helper.CanDeleteAsync("SampleTypes", id);

            if (!validation.CanDelete)
                return (false, validation.Message);

            var sampleType = await this.context.SampleTypes.FindAsync(id);
            if (sampleType == null)
                return (false, "Tipo de muestra no encontrado.");

            this.context.SampleTypes.Remove(sampleType);
            await this.context.SaveChangesAsync();

            return (true, "Tipo de muestra eliminado exitosamente.");
        }

#endregion


#region LAB TESTS
        //1.OBTENER EXAMENES
        public async Task<List<LabTest>> GetLabTestsAsync()
        {
            // Traemos también el color del tubo asociado
            return await this.context.LabTests
                                     .Include(t => t.SampleType)
                                     .OrderBy(t => t.TestName)
                                     .ToListAsync();
        }
        //2.Obtener examen por ID
        public async Task<LabTest?> GetLabTestByIdAsync(int id)
        {
            return await this.context.LabTests
                .Include(t => t.SampleType)
                .Include(t => t.ReferenceRanges)
                .FirstOrDefaultAsync(t => t.TestID == id);
        }
        //3.Crear un examen
        public async Task<LabTest> CreateLabTestAsync(string testName, string? units, int sampleId)
        {
            int newId = await helper.GetNextIdAsync("LabTests");

            LabTest labTest = new LabTest
            {
                TestID = newId,
                TestName = testName,
                Units = units,
                SampleID = sampleId
            };

            this.context.LabTests.Add(labTest);
            await this.context.SaveChangesAsync();

            return labTest;
        }
        //4.Actualizar un examen
        public async Task<bool> UpdateLabTestAsync(LabTest labTest)
        {
            var existing = await this.context.LabTests.FindAsync(labTest.TestID);
            if (existing == null)
                return false;

            existing.TestName = labTest.TestName;
            existing.Units = labTest.Units;
            existing.SampleID = labTest.SampleID;

            await this.context.SaveChangesAsync();
            return true;
        }
        //5.Borrar un examen
        public async Task<(bool Success, string Message)> DeleteLabTestAsync(int id)
        {
            var validation = await this.helper.CanDeleteAsync("LabTests", id);

            if (!validation.CanDelete)
                return (false, validation.Message);

            var labTest = await this.context.LabTests.FindAsync(id);
            if (labTest == null)
                return (false, "Examen no encontrado.");

            this.context.LabTests.Remove(labTest);
            await this.context.SaveChangesAsync();

            return (true, "Examen eliminado exitosamente.");
        }
        //6.Obtener exámenes por tipo de muestra
        public async Task<List<LabTest>> GetLabTestsBySampleTypeAsync(int sampleId)
        {
            return await this.context.LabTests
                .Where(t => t.SampleID == sampleId)
                .OrderBy(t => t.TestName)
                .ToListAsync();
        }

#endregion

#region RANGOS DE REFERENCIA
        //1.Obtener todos los rangos de referencia
        public async Task<List<ReferenceRange>> GetReferenceRangesAsync()
        {
            return await this.context.ReferenceRanges
                .Include(rr => rr.LabTest)
                .OrderBy(rr => rr.LabTest.TestName)
                .ThenBy(rr => rr.Gender)
                .ThenBy(rr => rr.MinAgeYear)
                .ToListAsync();
        }
        //2.Obtener rango por ID
        public async Task<ReferenceRange?> GetReferenceRangeByIdAsync(int id)
        {
            return await this.context.ReferenceRanges
                .Include(rr => rr.LabTest)
                .FirstOrDefaultAsync(rr => rr.RangeID == id);
        }
        //3.Obtener rangos de referencia por examen
        public async Task<List<ReferenceRange>> GetReferenceRangesByTestAsync(int testId)
        {
            return await this.context.ReferenceRanges
                .Where(rr => rr.TestID == testId)
                .OrderBy(rr => rr.Gender)
                .ThenBy(rr => rr.MinAgeYear)
                .ToListAsync();
        }
        //4.Crear rango de referencia(no se usará por ahora)
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
                MaxVal = maxVal
            };

            this.context.ReferenceRanges.Add(referenceRange);
            await this.context.SaveChangesAsync();

            return referenceRange;
        }
        //5.Actualizar rango de renferencia
        public async Task<bool> UpdateReferenceRangeAsync(ReferenceRange referenceRange)
        {
            var existing = await this.context.ReferenceRanges.FindAsync(referenceRange.RangeID);
            if (existing == null)
                return false;

            existing.TestID = referenceRange.TestID;
            existing.Gender = referenceRange.Gender;
            existing.MinAgeYear = referenceRange.MinAgeYear;
            existing.MaxAgeYear = referenceRange.MaxAgeYear;
            existing.MinVal = referenceRange.MinVal;
            existing.MaxVal = referenceRange.MaxVal;

            await this.context.SaveChangesAsync();
            return true;
        }
        //6.Eliminar rango
        public async Task<(bool Success, string Message)> DeleteReferenceRangeAsync(int id)
        {
            var referenceRange = await this.context.ReferenceRanges.FindAsync(id);
            if (referenceRange == null)
                return (false, "Rango de referencia no encontrado.");

            this.context.ReferenceRanges.Remove(referenceRange);
            await this.context.SaveChangesAsync();

            return (true, "Rango de referencia eliminado exitosamente.");
        }
#endregion




    }
}