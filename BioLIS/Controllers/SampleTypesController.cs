using BioLIS.Filters;
using BioLIS.Models.Entities;
using BioLIS.Services;
using Microsoft.AspNetCore.Mvc;

namespace BioLIS.Controllers
{
    [AuthorizeUsers(Policy = "AdminOnly")]
    public class SampleTypesController : Controller
    {
        private readonly ApiService api;
        public SampleTypesController(ApiService api) => this.api = api;

        public async Task<IActionResult> Index()
        {
            var st    = await this.api.GetSampleTypesAsync() ?? new();
            var tests = await this.api.GetLabTestsAsync()    ?? new();
            ViewData["UsageStats"] = st.Select(s => new { SampleType = s, TestCount = tests.Count(t => t.SampleID == s.SampleID) }).ToList();
            return View(st);
        }

        public IActionResult Create() => View();

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string sampleName, string containerColor)
        {
            if (string.IsNullOrWhiteSpace(sampleName))
            { TempData["ErrorMessage"] = "El nombre es obligatorio."; return RedirectToAction("Create"); }
            var r = await this.api.CreateSampleTypeAsync(sampleName, containerColor);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Tipo creado" : "Error";
            TempData["SwalMessage"] = r.Success ? $"'{sampleName}' creado." : r.Body;
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(int sampleId)
        {
            var st = await this.api.GetSampleTypeByIdAsync(sampleId);
            if (st == null) return NotFound();
            return View(st);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int sampleId, string sampleName, string? containerColor)
        {
            var r = await this.api.UpdateSampleTypeAsync(sampleId, sampleName, containerColor);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Actualizado" : "Error";
            TempData["SwalMessage"] = r.Body;
            return RedirectToAction(r.Success ? "Index" : "Update", r.Success ? null : new { sampleId });
        }

        public async Task<IActionResult> Delete(int sampleId)
        {
            var st = await this.api.GetSampleTypeByIdAsync(sampleId);
            if (st == null) return NotFound();
            ViewData["RelatedTests"] = await this.api.GetLabTestsBySampleTypeAsync(sampleId) ?? new();
            return View(st);
        }

        [HttpPost, ActionName("Delete")][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int sampleId)
        {
            var r = await this.api.DeleteSampleTypeAsync(sampleId);
            TempData["SwalType"]    = r.Success ? "success" : "error";
            TempData["SwalTitle"]   = r.Success ? "Eliminado" : "No se pudo eliminar";
            TempData["SwalMessage"] = r.Body;
            return RedirectToAction("Index");
        }
    }
}
