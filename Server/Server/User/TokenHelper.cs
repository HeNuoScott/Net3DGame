using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

namespace Server
{
    public static class TokenHelper
    {
        private static readonly string key = "server";
        private static readonly string iv = "henuo";

        /// <summary>   
        /// 加密方法   
        /// </summary>   
        /// <param name="Source">待加密的串</param>   
        /// <returns>经过加密的串</returns> 
        public static string GenToken(string account)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider
            {
                Key = Encoding.ASCII.GetBytes(key),
                IV = Encoding.ASCII.GetBytes(iv)
            };
            byte[] inputByteArray = Encoding.Default.GetBytes(account);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.Flush();
            cs.FlushFinalBlock();
            ms.Close();
            byte[] bytOut = ms.ToArray();
            return Convert.ToBase64String(bytOut);
        }

        /// <summary>   
        /// 解密方法   
        /// </summary>   
        /// <param name="Source">待解密的串</param>   
        /// <returns>经过解密的串</returns>   
        public static string DecodeToken(string token)
        {
            byte[] bytIn = Convert.FromBase64String(token);
            MemoryStream ms = new MemoryStream(bytIn, 0, bytIn.Length);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider
            {
                Key = Encoding.ASCII.GetBytes(key),
                IV = Encoding.ASCII.GetBytes(iv)
            };
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cs);
            string strcontent = sr.ReadToEnd();
            sr.Close();
            string[] accountData = strcontent.Split('_');
            return strcontent;
        }
    }
}
