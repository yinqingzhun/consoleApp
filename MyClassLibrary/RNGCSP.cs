using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MyClassLibrary
{
    public class RNGCSP
    {
        // Main method.
        public static void Run()
        {
            // Roll the dice 30 times and display 
            // the results to the console.
            for (int x = 0; x <= 30; x++)
                Console.WriteLine(RollDice(6));
        }

        // This method simulates a roll of the dice. The input parameter is the 
        // number of sides of the dice.
        public static int RollDice(int NumSides)
        {
            // Create a byte array to hold the random value.
            byte[] randomNumber = new byte[1];

            // Create a new instance of the RNGCryptoServiceProvider. 
            RNGCryptoServiceProvider Gen = new RNGCryptoServiceProvider();

            // Fill the array with a random value.
            Gen.GetBytes(randomNumber);

            // Convert the byte to an integer value to make the modulus operation easier.
            int rand = Convert.ToInt32(randomNumber[0]);

            // Return the random number mod the number
            // of sides.  The possible values are zero-
            // based, so we add one.
            return rand % NumSides + 1;
        }
        /// <summary>
        /// 产生指定长度的十六进制形式的字符串
        /// </summary>
        /// <param name="len"></param>
        /// <returns></returns>
        public static string CreateKey(int len)
        {

            byte[] bytes = new byte[len];

            new RNGCryptoServiceProvider().GetBytes(bytes);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {

                sb.Append(string.Format("{0:X2}", bytes[i]));

            }

            return sb.ToString();

        }
    }
}
