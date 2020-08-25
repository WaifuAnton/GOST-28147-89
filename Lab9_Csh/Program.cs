using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Lab9_Csh
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string testString = "Hello, how are";
                byte[] testBytes = Encoding.UTF8.GetBytes(testString);
                for (int i = 0; i < testBytes.Length; i++)
                    Console.Write("{0, -5}", testBytes[i]);
                Console.WriteLine();
                uint[] key = { 0, 0, 1, 1, 0, 0, 1, 1 };
                byte[] ssylka = { 1, 1, 1, 1, 1, 0, 0, 0 };
                Gost gost = new Gost(key, ssylka);
                byte[] encrypted = gost.EncryptECB(testBytes);
                for (int i = 0; i < encrypted.Length; i++)
                    Console.Write("{0, -5}", encrypted[i]);
                Console.WriteLine();
                Console.WriteLine();
                byte[] decrypted = gost.DecryptECB(encrypted);
                byte[][] gammas = gost.GetGammas();
                //for (int i = 0; i < gammas.Length; i++)
                //{
                //    for (int j = 0; j < gammas[i].Length; j++)
                //        Console.Write("{0, -5}", gammas[i][j]);
                //    Console.WriteLine();
                //}
                testString = Encoding.UTF8.GetString(decrypted);
                Console.WriteLine(testString);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
}
