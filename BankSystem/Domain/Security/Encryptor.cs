using System.Collections.Generic;
using System.Linq;

namespace Domain
{
    public static class Encryptor
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private static List<char> BuildCipher(string key)
        {
            List<char> cipher = new List<char>(36);

            if (string.IsNullOrWhiteSpace(key) || key.Length > Alphabet.Length)
                return cipher;

            key = key.ToUpperInvariant();

            HashSet<char> seen = new HashSet<char>();

            foreach (char c in key)
            {
                if (Alphabet.Contains(c) && seen.Add(c))
                    cipher.Add(c);
            }

            foreach (char c in Alphabet)
            {
                if (seen.Add(c))
                    cipher.Add(c);
            }

            return cipher;
        }

        private static Dictionary<char, char> CreateEncryptionLUT(string key)
        {
            List<char> cipher = BuildCipher(key);

            if (cipher.Count != Alphabet.Length)
                return new Dictionary<char, char>(0);

            Dictionary<char, char> lut = new Dictionary<char, char>(Alphabet.Length);

            for (int i = 0; i < Alphabet.Length; i++)
                lut.Add(Alphabet[i], cipher[i]);

            return lut;
        }

        private static Dictionary<char, char> CreateDecryptionLUT(string key)
        {
            List<char> cipher = BuildCipher(key);

            if (cipher.Count != Alphabet.Length)
                return new Dictionary<char, char>(0);

            Dictionary<char, char> lut = new Dictionary<char, char>(Alphabet.Length);

            for (int i = 0; i < Alphabet.Length; i++)
                lut.Add(cipher[i], Alphabet[i]);

            return lut;
        }

        public static string Encrypt(string key, string input)
        {
            if (string.IsNullOrWhiteSpace(key) || input == null)
                return null;

            Dictionary<char, char> lut = CreateEncryptionLUT(key);

            if (lut.Count == 0)
                return null;

            char[] result = new char[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                char original = input[i];
                char c = char.ToUpperInvariant(original);

                if (lut.TryGetValue(c, out char mapped))
                {
                    if (char.IsLetter(original) && char.IsLower(original))
                        mapped = char.ToLowerInvariant(mapped);

                    result[i] = mapped;
                }
                else
                {
                    result[i] = original;
                }
            }

            return new string(result);
        }

        public static string Decrypt(string key, string input)
        {
            if (string.IsNullOrWhiteSpace(key) || input == null)
                return null;

            Dictionary<char, char> lut = CreateDecryptionLUT(key);

            if (lut.Count == 0)
                return null;

            char[] result = new char[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                char original = input[i];
                char c = char.ToUpperInvariant(original);

                if (lut.TryGetValue(c, out char mapped))
                {
                    if (char.IsLetter(original) && char.IsLower(original))
                        mapped = char.ToLowerInvariant(mapped);

                    result[i] = mapped;
                }
                else
                {
                    result[i] = original;
                }
            }

            return new string(result);
        }
    }
}
