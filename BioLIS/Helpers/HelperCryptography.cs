using System.Security.Cryptography;
using System.Text;

namespace BioLIS.Helpers
{
    public class HelperCryptography
    {
        public static byte[] EncryptPassword(string password, string salt)
        {
            string contenido = salt + password + salt;

            SHA512 managed = SHA512.Create();

            byte[] salida = Encoding.UTF8.GetBytes(contenido);

            for (int i = 0; i < 7; i++)
            {//si lo hacemos async hay que pasarle string y luego convertirlo a byte y eso
                salida = managed.ComputeHash(salida);
            }
            managed.Clear();

            return salida;

        }
    }
}
