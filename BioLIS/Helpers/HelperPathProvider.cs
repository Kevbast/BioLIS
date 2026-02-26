namespace BioLIS.Helpers
{
    public enum Folders { Pacientes,Barcodes,Users}
    public class HelperPathProvider
    {
        private IWebHostEnvironment environment;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public HelperPathProvider(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            this.environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }
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
            string path = Path.Combine(rootPath,"images", carpeta, fileName);

            return path;

        }



    }
}
