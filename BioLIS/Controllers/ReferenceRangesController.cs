using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BioLIS.Controllers
{
    [AuthorizeRole("Admin")] // Solo Admin puede gestionar rangos de referencia
    public class ReferenceRangesController : Controller
    {
        private readonly CatalogRepository catalogRepo;

        public ReferenceRangesController(CatalogRepository catalogRepo)
        {
            this.catalogRepo = catalogRepo;
        }

        // GET: ReferenceRanges
        public async Task<IActionResult> Index()
        {
            var ranges = await catalogRepo.GetAllReferenceRangesAsync();
            
            // Agrupar por examen para mejor visualización
            var groupedRanges = ranges.GroupBy(r => r.LabTest.TestName).ToList();
            ViewData["GroupedRanges"] = groupedRanges;
            
            return View(ranges);
        }

        // GET: ReferenceRanges/Create
        public async Task<IActionResult> Create()
        {
            // Obtener todos los exámenes
            var labTests = await catalogRepo.GetLabTestsAsync();
            ViewData["LabTests"] = labTests.Select(t => new SelectListItem
            {
                Value = t.TestID.ToString(),
                Text = $"{t.TestName} ({t.Units})"
            }).ToList();

            return View();
        }

        // POST: ReferenceRanges/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int testId, string gender, int minAgeYear, int maxAgeYear, 
                                                 decimal minVal, decimal maxVal)
        {
            // Validaciones
            if (minAgeYear < 0 || maxAgeYear > 120 || minAgeYear >= maxAgeYear)
            {
                TempData["ErrorMessage"] = "Rango de edad inválido. MinAge debe ser menor que MaxAge y entre 0-120 años.";
                return RedirectToAction("Create");
            }

            if (minVal >= maxVal)
            {
                TempData["ErrorMessage"] = "Rango de valores inválido. MinVal debe ser menor que MaxVal.";
                return RedirectToAction("Create");
            }

            if (!new[] { "M", "F", "A" }.Contains(gender))
            {
                TempData["ErrorMessage"] = "Género inválido. Use M (Masculino), F (Femenino) o A (Ambos).";
                return RedirectToAction("Create");
            }

            await catalogRepo.CreateReferenceRangeAsync(testId, gender, minAgeYear, maxAgeYear, minVal, maxVal);

            TempData["SuccessMessage"] = "Rango de referencia creado exitosamente.";
            return RedirectToAction("Index");
        }

        // GET: ReferenceRanges/Update/5
        public async Task<IActionResult> Update(int rangeId)
        {
            var range = await catalogRepo.GetReferenceRangeByIdAsync(rangeId);
            if (range == null)
            {
                return NotFound();
            }

            // Obtener todos los exámenes para el dropdown
            var labTests = await catalogRepo.GetLabTestsAsync();
            ViewData["LabTests"] = labTests.Select(t => new SelectListItem
            {
                Value = t.TestID.ToString(),
                Text = $"{t.TestName} ({t.Units})",
                Selected = t.TestID == range.TestID
            }).ToList();

            return View(range);
        }

        // POST: ReferenceRanges/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ReferenceRange range)
        {
            // Validaciones
            if (range.MinAgeYear < 0 || range.MaxAgeYear > 120 || range.MinAgeYear >= range.MaxAgeYear)
            {
                TempData["ErrorMessage"] = "Rango de edad inválido.";
                
                // Recargar dropdown
                var labTests = await catalogRepo.GetLabTestsAsync();
                ViewData["LabTests"] = labTests.Select(t => new SelectListItem
                {
                    Value = t.TestID.ToString(),
                    Text = $"{t.TestName} ({t.Units})"
                }).ToList();
                
                return View(range);
            }

            if (range.MinVal >= range.MaxVal)
            {
                TempData["ErrorMessage"] = "Rango de valores inválido. MinVal debe ser menor que MaxVal.";
                
                var labTests = await catalogRepo.GetLabTestsAsync();
                ViewData["LabTests"] = labTests.Select(t => new SelectListItem
                {
                    Value = t.TestID.ToString(),
                    Text = $"{t.TestName} ({t.Units})"
                }).ToList();
                
                return View(range);
            }

            bool success = await catalogRepo.UpdateReferenceRangeAsync(range);

            if (success)
            {
                TempData["SuccessMessage"] = "Rango de referencia actualizado exitosamente.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["ErrorMessage"] = "Error al actualizar el rango de referencia.";
                return View(range);
            }
        }

        // GET: ReferenceRanges/Delete/5
        public async Task<IActionResult> Delete(int rangeId)
        {
            var range = await catalogRepo.GetReferenceRangeByIdAsync(rangeId);
            if (range == null)
            {
                return NotFound();
            }

            return View(range);
        }

        // POST: ReferenceRanges/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int rangeId)
        {
            var result = await catalogRepo.DeleteReferenceRangeAsync(rangeId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
