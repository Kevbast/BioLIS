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



    }
}
