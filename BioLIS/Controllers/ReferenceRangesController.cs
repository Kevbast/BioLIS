using BioLIS.Filters;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BioLIS.Controllers
{
    [AuthorizeUsers(Policy = "AdminOnly")]
    public class ReferenceRangesController : Controller
    {
        private readonly ApiService api;
        public ReferenceRangesController(ApiService api) => this.api = api;

        public async Task<IActionResult> Index()
        {
            var ranges = await this.api.GetAllReferenceRangesAsync() ?? new();
            ViewData["GroupedRanges"] = ranges.Where(r => r.LabTest != null)
                .GroupBy(r => r.LabTest.TestName).ToList();
            return View(ranges);
        }

        public async Task<IActionResult> Create()
        { await LoadLabTests(); return View(); }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int testId, string gender,
            int minAgeYear, int maxAgeYear, decimal minVal, decimal maxVal)
        {
            if (!new[] { "M","F","A" }.Contains(gender))
            { TempData["ErrorMessage"] = "Género inválido. Use M, F o A."; return RedirectToAction("Create"); }
            if (minAgeYear < 0 || maxAgeYear > 120 || minAgeYear >= maxAgeYear)
            { TempData["ErrorMessage"] = "Rango de edad inválido."; return RedirectToAction("Create"); }
            if (minVal >= maxVal)
            { TempData["ErrorMessage"] = "MinVal debe ser menor que MaxVal."; return RedirectToAction("Create"); }

            var r = await this.api.CreateReferenceRangeAsync(testId, gender, minAgeYear, maxAgeYear, minVal, maxVal);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Rango creado" : "Error";
            TempData["SwalMessage"] = r.Success ? "Rango de referencia creado." : r.Body;
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(int rangeId)
        {
            var rr = await this.api.GetReferenceRangeByIdAsync(rangeId);
            if (rr == null) return NotFound();
            await LoadLabTests(rr.TestID);
            return View(rr);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ReferenceRange range)
        {
            if (range.MinAgeYear >= range.MaxAgeYear)
            { TempData["ErrorMessage"] = "Rango de edad inválido."; return RedirectToAction("Update", new { rangeId = range.RangeID }); }
            if (range.MinVal >= range.MaxVal)
            { TempData["ErrorMessage"] = "MinVal debe ser menor que MaxVal."; return RedirectToAction("Update", new { rangeId = range.RangeID }); }

            var r = await this.api.UpdateReferenceRangeAsync(
                range.RangeID, range.TestID, range.Gender,
                range.MinAgeYear, range.MaxAgeYear, range.MinVal, range.MaxVal);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Actualizado" : "Error";
            TempData["SwalMessage"] = r.Body;
            return RedirectToAction(r.Success ? "Index" : "Update", r.Success ? null : new { rangeId = range.RangeID });
        }

        public async Task<IActionResult> Delete(int rangeId)
        {
            var rr = await this.api.GetReferenceRangeByIdAsync(rangeId);
            if (rr == null) return NotFound();
            return View(rr);
        }

        [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int rangeId)
        {
            var r = await this.api.DeleteReferenceRangeAsync(rangeId);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Eliminado" : "No se pudo eliminar";
            TempData["SwalMessage"] = r.Body;
            return RedirectToAction("Index");
        }

        private async Task LoadLabTests(int? selectedId = null)
        {
            var lt = await this.api.GetLabTestsAsync() ?? new();
            ViewData["LabTests"] = lt.Select(t => new SelectListItem
            {
                Value    = t.TestID.ToString(),
                Text     = $"{t.TestName} ({t.Units})",
                Selected = t.TestID == selectedId
            }).ToList();
        }
    }
}
