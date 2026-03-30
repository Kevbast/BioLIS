using BioLIS.Models;
using BioLIS.Filters;
using BioLIS.Helpers;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BioLIS.Controllers
{
    [AuthorizeUsers]
    public class PatientsController : Controller
    {
        private CatalogRepository repo;
        private OrderRepository orderRepo;
        private HelperPathProvider pathHelper;

        public PatientsController(CatalogRepository repo, OrderRepository orderRepo, HelperPathProvider pathHelper)
        {
            this.repo = repo;
            this.orderRepo = orderRepo;
            this.pathHelper = pathHelper;
        }

        public async Task<IActionResult> Index()
        {
            List<Patient> pacientes = await this.repo.GetPatientsAsync();
            return View(pacientes);
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Inactive()
        {
            var pacientes = await this.repo.GetInactivePatientsAsync();
            return View(pacientes);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Patient patient, IFormFile fichero)
        {
            string nombreImagen = "default.png";

            if (fichero != null)
            {
                nombreImagen = fichero.FileName;
                string path = this.pathHelper.MapPath(nombreImagen, Folders.Pacientes);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await fichero.CopyToAsync(stream);
                }
            }

            // Extraer el ID del usuario actual para la Auditoría
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

            await this.repo.CreatePatientAsync(
                patient.FirstName,
                patient.LastName,
                patient.Gender,
                patient.BirthDate,
                patient.Email,
                nombreImagen,
                patient.PhoneNumber, // NUEVO: Teléfono
                currentUserId        // NUEVO: Auditoría
            );

            TempData["SwalType"] = "success";
            TempData["SwalTitle"] = "Paciente registrado";
            TempData["SwalMessage"] = $"Se registró correctamente a {patient.FirstName} {patient.LastName}.";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(int patientId)
        {
            Patient paciente = await this.repo.GetPatientByIdAsync(patientId);
            if (paciente == null) return RedirectToAction("Index");
            return View(paciente);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Patient patient, IFormFile fichero)
        {
            string nombreImagen = patient.PhotoFilename ?? "default.png";

            if (fichero != null)
            {
                nombreImagen = fichero.FileName;
                string path = this.pathHelper.MapPath(nombreImagen, Folders.Pacientes);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await fichero.CopyToAsync(stream);
                }
            }

            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

            bool exito = await this.repo.UpdatePatientAsync(
                patient.PatientID,
                patient.FirstName,
                patient.LastName,
                patient.Gender,
                patient.BirthDate,
                patient.Email,
                nombreImagen,
                patient.PhoneNumber, // NUEVO: Teléfono
                currentUserId        // NUEVO: Auditoría
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
                return View(patient);
            }
        }

        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int patientId)
        {
            Patient paciente = await this.repo.GetPatientByIdAsync(patientId);
            if (paciente == null)
            {
                return RedirectToAction("Index");
            }

            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

            var result = await this.repo.DeletePatientAsync(patientId, currentUserId);

            if (result.Success)
            {
                // NOTA: Borrado de archivo físico omitido por ser Soft Delete
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Paciente desactivado";
                TempData["SwalMessage"] = "El paciente fue desactivado correctamente.";
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo eliminar";
                TempData["SwalMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUsers(Policy = "AdminOnly")]
        public async Task<IActionResult> Reactivate(int patientId)
        {
            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

            var result = await this.repo.ReactivatePatientAsync(patientId, currentUserId);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Paciente reactivado";
                TempData["SwalMessage"] = result.Message;
            }
            else
            {
                TempData["SwalType"] = "error";
                TempData["SwalTitle"] = "No se pudo reactivar";
                TempData["SwalMessage"] = result.Message;
            }

            return RedirectToAction("Inactive");
        }

        [AuthorizeUsers(Policy = "AllRoles")] // Médicos, Labs y Admins pueden ver el historial
        public async Task<IActionResult> History(int patientId)
        {
            var patient = await this.repo.GetPatientByIdAsync(patientId);
            if (patient == null) return NotFound();

            var rawHistory = await this.orderRepo.GetPatientHistoryAsync(patientId) ?? new List<TestResult>();

            // Formateamos los datos para enviarlos limpios al JavaScript de la Vista
            var historyData = rawHistory
                .Where(h => h?.Order != null && h.LabTest != null)
                .Select(h => new
                {
                    Date = h.Order.OrderDate.ToString("dd/MM/yyyy"),
                    TestName = h.LabTest.TestName,
                    Value = h.ResultValue,
                    Units = h.LabTest.Units
                })
                .ToList();

            // Sacamos una lista única de los exámenes que se ha hecho este paciente
            ViewData["AvailableTests"] = historyData
                .Where(h => !string.IsNullOrWhiteSpace(h.TestName))
                .Select(h => h.TestName)
                .Distinct()
                .ToList();

            // Serializamos los datos a JSON
            ViewData["HistoryJson"] = System.Text.Json.JsonSerializer.Serialize(historyData);

            return View(patient);
        }
    }
}