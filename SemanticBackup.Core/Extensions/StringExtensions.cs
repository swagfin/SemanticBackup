﻿using System.Text;
using System.Text.RegularExpressions;

namespace SemanticBackup.Core
{
    public static class StringExtensions
    {
        public static string ToMD5String(this string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));
                return sb.ToString();
            }
        }
        public static string FormatToUrlStyle(this string input)
        {
            return Regex.Replace(input, @"[^a-zA-Z0-9]+", "-").Trim('-').ToLower().Trim();
        }
    }
}
