using BioLab.Models;
using BioLIS.Filters;
using BioLIS.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BioLIS.Controllers
{
    [AuthorizeRole("Admin")] // Solo Admin puede gestionar tipos de muestra
    public class SampleTypesController : Controller
    {
        private readonly CatalogRepository catalogRepo;

        public SampleTypesController(CatalogRepository catalogRepo)
        {
            this.catalogRepo = catalogRepo;
        }

        // GET: SampleTypes
        public async Task<IActionResult> Index()
        {
            var sampleTypes = await catalogRepo.GetSampleTypesAsync();
            
            // Obtener estadísticas de uso
            var allTests = await catalogRepo.GetLabTestsAsync();
            var usageStats = sampleTypes.Select(st => new
            {
                SampleType = st,
                TestCount = allTests.Count(t => t.SampleID == st.SampleID)
            }).ToList();

            ViewData["UsageStats"] = usageStats;
            
            return View(sampleTypes);
        }

        // GET: SampleTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SampleTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string sampleName, string containerColor)
        {
            if (string.IsNullOrWhiteSpace(sampleName))
            {
                TempData["ErrorMessage"] = "El nombre del tipo de muestra es obligatorio.";
                return RedirectToAction("Create");
            }

            await catalogRepo.CreateSampleTypeAsync(sampleName, containerColor);

            TempData["SuccessMessage"] = $"Tipo de muestra '{sampleName}' creado exitosamente.";
            return RedirectToAction("Index");
        }

        // GET: SampleTypes/Update/5
        public async Task<IActionResult> Update(int sampleId)
        {
            var sampleType = await catalogRepo.GetSampleTypeByIdAsync(sampleId);
            if (sampleType == null)
            {
                return NotFound();
            }

            return View(sampleType);
        }

        // POST: SampleTypes/Update/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(SampleType sampleType)
        {
            if (ModelState.IsValid)
            {
                bool success = await catalogRepo.UpdateSampleTypeAsync(sampleType);

                if (success)
                {
                    TempData["SuccessMessage"] = "Tipo de muestra actualizado exitosamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al actualizar el tipo de muestra.";
                }
            }

            return View(sampleType);
        }

        // GET: SampleTypes/Delete/5
        public async Task<IActionResult> Delete(int sampleId)
        {
            var sampleType = await catalogRepo.GetSampleTypeByIdAsync(sampleId);
            if (sampleType == null)
            {
                return NotFound();
            }

            // Obtener exámenes que usan este tipo de muestra
            var relatedTests = await catalogRepo.GetLabTestsBySampleTypeAsync(sampleId);
            ViewData["RelatedTests"] = relatedTests;

            return View(sampleType);
        }

        // POST: SampleTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int sampleId)
        {
            var result = await catalogRepo.DeleteSampleTypeAsync(sampleId);

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
