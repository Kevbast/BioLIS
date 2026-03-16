using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BioLIS.Controllers
{
    [AuthorizeUsers(Policy = "AdminOnly")] // Solo Admin puede gestionar exámenes
    public class LabTestsController : Controller
    {
        private readonly CatalogRepository catalogRepo;

        public LabTestsController(CatalogRepository catalogRepo)
        {
            this.catalogRepo = catalogRepo;
        }

        // GET: LabTests
        public async Task<IActionResult> Index()
        {
            var labTests = await catalogRepo.GetLabTestsAsync();
            return View(labTests);
        }

        // GET: LabTests/Create
        public async Task<IActionResult> Create()
        {
            // Cargar tipos de muestra para el dropdown
            var sampleTypes = await catalogRepo.GetSampleTypesAsync();
            ViewData["SampleTypes"] = sampleTypes.Select(s => new SelectListItem
            {
                Value = s.SampleID.ToString(),
                Text = $"{s.SampleName} (Tubo {s.ContainerColor})"
            }).ToList();

            return View();
        }

        // POST: LabTests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string testName, string units, int sampleId)
        {
            if (string.IsNullOrWhiteSpace(testName))
            {
                TempData["ErrorMessage"] = "El nombre del examen es obligatorio.";
                return RedirectToAction("Create");
            }

            await catalogRepo.CreateLabTestAsync(testName, units, sampleId);

            TempData["SwalType"] = "success";
            TempData["SwalTitle"] = "Examen creado";
            TempData["SwalMessage"] = $"Examen '{testName}' creado exitosamente.";
            return RedirectToAction("Index");
        }

        // GET: LabTests/Update/5
        public async Task<IActionResult> Update(int testId)
        {
            var labTest = await catalogRepo.GetLabTestByIdAsync(testId);
            if (labTest == null)
            {
                return NotFound();
            }

            // Cargar tipos de muestra para el dropdown
            var sampleTypes = await catalogRepo.GetSampleTypesAsync();
            ViewData["SampleTypes"] = sampleTypes.Select(s => new SelectListItem
            {
                Value = s.SampleID.ToString(),
                Text = $"{s.SampleName} (Tubo {s.ContainerColor})",
                Selected = s.SampleID == labTest.SampleID
            }).ToList();

            return View(labTest);
        }

        // POST: LabTests/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int testId, string testName, string? units, int sampleId)
        {
            if (string.IsNullOrWhiteSpace(testName))
            {
                ModelState.AddModelError(nameof(testName), "El nombre del examen es obligatorio.");
            }

            if (sampleId <= 0)
            {
                ModelState.AddModelError(nameof(sampleId), "Debe seleccionar un tipo de muestra.");
            }

            if (ModelState.IsValid)
            {
                var labTest = new LabTest
                {
                    TestID = testId,
                    TestName = testName.Trim(),
                    Units = units?.Trim(),
                    SampleID = sampleId
                };

                bool success = await catalogRepo.UpdateLabTestAsync(labTest);

                if (success)
                {
                    TempData["SwalType"] = "success";
                    TempData["SwalTitle"] = "Examen actualizado";
                    TempData["SwalMessage"] = "Examen actualizado exitosamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["SwalType"] = "error";
                    TempData["SwalTitle"] = "Error de actualización";
                    TempData["SwalMessage"] = "Error al actualizar el examen.";
                }
            }

            // Recargar tipos de muestra si hay error
            var sampleTypes = await catalogRepo.GetSampleTypesAsync();
            ViewData["SampleTypes"] = sampleTypes.Select(s => new SelectListItem
            {
                Value = s.SampleID.ToString(),
                Text = $"{s.SampleName} (Tubo {s.ContainerColor})",
                Selected = s.SampleID == sampleId
            }).ToList();

            var existingLabTest = await catalogRepo.GetLabTestByIdAsync(testId);
            if (existingLabTest == null)
            {
                return NotFound();
            }

            existingLabTest.TestName = testName;
            existingLabTest.Units = units;
            existingLabTest.SampleID = sampleId;

            return View(existingLabTest);
        }

        // GET: LabTests/Delete/5
        public async Task<IActionResult> Delete(int testId)
        {
            var labTest = await catalogRepo.GetLabTestByIdAsync(testId);
            if (labTest == null)
            {
                return NotFound();
            }

            return View(labTest);
        }

        // POST: LabTests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int testId)
        {
            var result = await catalogRepo.DeleteLabTestAsync(testId);

            if (result.Success)
            {
                TempData["SwalType"] = "success";
                TempData["SwalTitle"] = "Examen eliminado";
                TempData["SwalMessage"] = result.Message;
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
