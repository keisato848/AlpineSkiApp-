using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace PointApp.Views

{
    public static class DatabaseUtility
    {
        private static string GetDatabaseSetting()
        {
            string strJson = null;
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var jsonResource = assembly.GetManifestResourceNames();
                var file = assembly.GetManifestResourceStream(jsonResource.First(json => json.Equals("PointApp.Settings.json")));
                if (file != null)
                {
                    using (var sr = new StreamReader(file))
                    {
                        strJson = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return strJson;
        }

        public static string GetConnectInfo()
        {
            string connectInfo = null;
            try
            {
                string strJson = GetDatabaseSetting();
                if (!string.IsNullOrEmpty(strJson))
                {
                    var listJson = JsonSerializer.Deserialize<List<JsonElement>>(strJson);
                    foreach (var json in listJson)
                    {
                        if (!string.IsNullOrEmpty(json.DatabaseInfo))
                        {
                            connectInfo = json.DatabaseInfo;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return connectInfo;
        }

        public static NpgsqlConnection ConnectDataBase()
        {
            NpgsqlConnection connection = null;
            try
            {
                var connectInfo = GetConnectInfo();
                if (connectInfo != null)
                {
                    connection = new NpgsqlConnection(connectInfo);
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine(ex);
            }
            return connection;
        }

        public static NpgsqlDataReader ExecuteSql(string sql, NpgsqlConnection connection)
        {
            NpgsqlDataReader reader = null;
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    reader = command.ExecuteReader();
                }
            }
            catch (Exception)
            {
                if (connection != null)
                {
                    connection.Close();
                }
                if (reader != null)
                {
                    reader.Close();
                }
                throw;
            }
            return reader;
        }

        public static void ExecuteSqlNonquery(string sql, NpgsqlConnection connection)
        {
            NpgsqlDataReader reader = null;
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                if (connection != null)
                {
                    connection.Close();
                }
                if (reader != null)
                {
                    reader.Close();
                }
                throw ex;
            }
        }

        public static object ExecuteScalar(string sql, NpgsqlConnection connection)
        {
            NpgsqlDataReader reader = null;
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    return command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                if (connection != null)
                {
                    connection.Close();
                }
                if (reader != null)
                {
                    reader.Close();
                }
                throw ex;
            }
        }

        public static string GetSalt()
        {
            string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder result = new StringBuilder();
            int length = 8;
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];
                for (int ii = 0; ii < length; ++ii)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    result.Append(valid[(int)(num % (uint)valid.Length)]);
                }
            }
            return result.ToString();
        }

        public static string GetSHA256HashString(string password, string salt)
        {
            // 平文のパスワードの末尾にサルトを結合 
            string passwordAndSalt = password + salt;
            // 文字列をバイト型配列に変換 
            byte[] data = Encoding.UTF8.GetBytes(passwordAndSalt);
            // SHA512ハッシュアルゴリズム生成 
            var algorithm = new SHA256CryptoServiceProvider();
            // ハッシュ値を計算 
            byte[] bs = algorithm.ComputeHash(data);
            // リソースを解放 
            algorithm.Clear();
            // バイト型配列を16進数文字列に変換 
            var result = new StringBuilder();
            foreach (byte b in bs)
            {
                result.Append(b.ToString("X2"));
            }
            return result.ToString();
        }

        public class JsonElement
        {
            public string DatabaseInfo { get; set; } = string.Empty;
        }
    }
}
