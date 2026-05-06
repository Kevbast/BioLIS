using System;
using System.Security.Cryptography;

namespace BioLIS.Helpers
{
    public class HelperTools
    {
        public static string GenerateSalt()
        {
            byte[] randomBytes = new byte[50];

            // Genera un salt criptográficamente seguro y lo convierte a Base64.
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            return Convert.ToBase64String(randomBytes);
        }

        public static bool CompareArrays(byte[] a, byte[] b)
        {
            bool iguales = true;

            if (a.Length != b.Length)
            {
                iguales = false;
            }
            else
            {
                // Compara byte a byte para validar igualdad.
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i].Equals(b[i]) == false)
                    {
                        iguales = false;
                        break;
                    }
                }
            }

            return iguales;
        }
    }
}