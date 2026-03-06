namespace BioLIS.Helpers
{
    public enum Folders { Pacientes, Barcodes, Users }

    public class HelperPathProvider
    {
        private IWebHostEnvironment environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HelperPathProvider(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            this.environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Devuelve la ruta física del servidor para guardar archivos
        /// Ejemplo: C:\proyecto\wwwroot\images\pacientes\foto.png
        /// </summary>
        public string MapPath(string fileName, Folders folder)
        {
            string carpeta = "";
            if (folder == Folders.Pacientes)
            {
                carpeta = "pacientes";
            }
            else if (folder == Folders.Barcodes)
            {
                carpeta = "barcodes";
            }
            else if (folder == Folders.Users)
            {
                carpeta = "users";
            }

            string rootPath = this.environment.WebRootPath;
            string path = Path.Combine(rootPath, "images", carpeta, fileName);

            return path;
        }

        /// <summary>
        /// Devuelve la URL web para mostrar archivos en HTML
        /// Ejemplo: https://localhost:7282/images/pacientes/foto.png
        /// </summary>
        public string MapUrlPath(string fileName, Folders folder)
        {
            string carpeta = "";

            // Recuperamos la URL del servidor dinámicamente
            var request = _httpContextAccessor.HttpContext.Request;
            string serverUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

            if (folder == Folders.Pacientes)
            {
                carpeta = "images/pacientes";
            }
            else if (folder == Folders.Barcodes)
            {
                carpeta = "images/barcodes";
            }
            else if (folder == Folders.Users)
            {
                carpeta = "images/users";
            }

            return $"{serverUrl}/{carpeta}/{fileName}";
        }
    }
}
