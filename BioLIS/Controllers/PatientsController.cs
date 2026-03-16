using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Helpers;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BioLIS.Controllers
{
    [AuthorizeUsers] // Solo Admin y Laboratorio pueden gestionar pacientes (Policy = "AdminOrLab") y doctores tmb
    public class PatientsController : Controller
    {
        private CatalogRepository repo;
        private HelperPathProvider pathHelper;

        public PatientsController(CatalogRepository repo, HelperPathProvider pathHelper)
        {
            this.repo = repo;
            this.pathHelper = pathHelper;
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

                // Usamos HelperPathProvider para obtener la ruta física
                string path = this.pathHelper.MapPath(nombreImagen, Folders.Pacientes);

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

            TempData["SwalType"] = "success";
            TempData["SwalTitle"] = "Paciente registrado";
            TempData["SwalMessage"] = $"Se registró correctamente a {patient.FirstName} {patient.LastName}.";

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
            string nombreImagen = patient.PhotoFilename ?? "default.png"; // Mantener imagen actual si no se sube nueva

            if (fichero != null)
            {
                // Generamos un nombre limpio o usamos el original
                nombreImagen = fichero.FileName;

                // Usamos HelperPathProvider para obtener la ruta física
                string path = this.pathHelper.MapPath(nombreImagen, Folders.Pacientes);

                // Subimos el archivo físicamente
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await fichero.CopyToAsync(stream);
                }
            }

            bool exito = await this.repo.UpdatePatientAsync(
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
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Paciente actualizado";
                TempData["SwalMessage"] = $"Los datos de {patient.FirstName} {patient.LastName} fueron actualizados.";
                return RedirectToAction("Index");
            }
            else
            {
                ViewData["MENSAJE"] = "Error al actualizar al paciente";
                return View();
            }
        }

        [AuthorizeUsers(Policy = "AdminOnly")] // Solo Admin puede eliminar pacientes
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
                    // Usamos HelperPathProvider para obtener la ruta física
                    string path = this.pathHelper.MapPath(nombreImagen, Folders.Pacientes);

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }

                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Paciente eliminado";
                TempData["SwalMessage"] = "El paciente fue eliminado correctamente.";
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo eliminar";
                TempData["SwalMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
