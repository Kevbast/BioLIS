using System;
using System.Security.Cryptography;

namespace BioLIS.Helpers
{
    public class HelperTools
    {
        public static string GenerateSalt()
        {
            // 1. Creamos un array de 50 bytes
            byte[] randomBytes = new byte[50];

            // 2. Llenamos el array con números aleatorios criptográficamente seguros
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // 3. Convertimos los bytes a Base64. 
            // Esto devolverá SOLO letras, números, y los símbolos '+' , '/' y '='.
            // ¡Jamás tendrá saltos de línea ni caracteres invisibles!
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
                // COMPARAMOS BYTE A BYTE
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