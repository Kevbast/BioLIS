using BioLab.Models;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BioLIS.Controllers
{
    public class PatientsController : Controller
    {
        private CatalogRepository repo;
        private IWebHostEnvironment environment;

        public PatientsController(CatalogRepository repo, IWebHostEnvironment environment)
        {
            this.repo = repo;
            this.environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            List<Patient> pacientes = await this.repo.GetPatientsAsync();
            return View(pacientes);
        }

        // GET: Mostrar el formulario de creación
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Patient patient, IFormFile fichero)
        {
            // 1. Gestión de la Imagen (Requisito clave)
            string nombreImagen = "default.png"; // Imagen por defecto si no suben nada

            if (fichero != null)
            {
                // Generamos un nombre limpio o usamos el original
                nombreImagen = fichero.FileName;

                // Definimos la ruta: wwwroot/images/pacientes
                string rootPath = environment.WebRootPath;
                string path = Path.Combine(rootPath, "images", "pacientes", nombreImagen);

                // Subimos el archivo físicamente
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await fichero.CopyToAsync(stream);
                }
            }

            await this.repo.CreatePatientAsync(
                patient.FirstName,
                patient.LastName,
                patient.Gender,
                patient.BirthDate,
                patient.Email,
                nombreImagen // Pasamos solo el nombre del archivo
            );

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(int patientId)
        {
            Patient paciente = await this.repo.GetPatientByIdAsync(patientId);
            return View(paciente);
        }

        [HttpPost]//REVISAR EN EL UPDATE LOS FILES SUBIDOS NO SÉ BORRAN LOS ANTIGUOS
        public async Task<IActionResult> Update(Patient patient, IFormFile fichero)
        {
            // 1. Gestión de la Imagen (Requisito clave)
            string nombreImagen = "default.png"; // Imagen por defecto si no suben nada

            if (fichero != null)
            {
                // Generamos un nombre limpio o usamos el original
                nombreImagen = fichero.FileName;

                // Definimos la ruta: wwwroot/images/pacientes
                string rootPath = environment.WebRootPath;
                string path = Path.Combine(rootPath, "images", "pacientes", nombreImagen);

                // Subimos el archivo físicamente
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await fichero.CopyToAsync(stream);
                }
            }

            bool exito= await this.repo.UpdatePatientAsync(
                patient.PatientID,
                patient.FirstName,
                patient.LastName,
                patient.Gender,
                patient.BirthDate,
                patient.Email,
                nombreImagen // Pasamos solo el nombre del archivo
            );

            if (exito)
            {
                return RedirectToAction("Index");
            }
            else
            {
                ViewData["MENSAJE"] = "Error al actualizar al paciente";
                return View();
            }

        }

        public async Task<IActionResult> Delete(int patientId)
        {
            Patient paciente = await this.repo.GetPatientByIdAsync(patientId);
            if (paciente == null)
            {
                return RedirectToAction("Index"); // O mostrar una vista de error
            }

            string nombreImagen = paciente.PhotoFilename;

            var result = await this.repo.DeletePatientAsync(patientId);

            // 3. Si se borró de la BD exitosamente, procedemos a borrar el archivo físico
            if (result.Success)
            {
                // Validamos que tenga foto y que no estemos borrando la foto por defecto
                if (!string.IsNullOrEmpty(nombreImagen) && nombreImagen != "default.png")
                {
                    string path = Path.Combine(this.environment.WebRootPath, "images", "pacientes", nombreImagen);

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }
            }


            return RedirectToAction("Index");

        }
        //DIVIDIMOS LOS CRUDS POR AHORA

        public async Task<IActionResult> Doctores()
        {
            List<Doctor> doctors = await this.repo.GetDoctorsAsync();
            return View(doctors);
        }





    }
}
