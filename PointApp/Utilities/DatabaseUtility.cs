using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace PointApp.Views

{
    public static class DatabaseUtility
    {
        /// <summary> 
        /// 設定ファイルのパスを取得します 
        /// </summary> 
        /// <returns>設定ファイルのフルパス</returns> 
        private static string GetDatabaseSettingPath()
        {
            string path = null;
            try
            {
                var myAssembly = Assembly.GetEntryAssembly();
                if (myAssembly != null)
                {
                    path = Path.Combine(myAssembly.Location, "../Settings.json");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return path;
        }
        /// <summary> 
        /// json ファイルからデータベースの接続先を取得 
        /// </summary> 
        /// <returns>接続先</returns> 
        public static string GetConnectInfo()
        {
            string connectInfo = null;
            try
            {
                string filePath = GetDatabaseSettingPath();
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                }
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string strJson = reader.ReadToEnd();
                        var listJson = JsonSerializer.Deserialize<List<JsonElement>>(strJson);
                        foreach (var json in listJson)
                        {
                            if (!string.IsNullOrEmpty(json.ConnectInfo))
                            {
                                connectInfo = json.ConnectInfo;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return connectInfo;
        }
        /// <summary> 
        /// データベースに接続します 
        /// </summary> 
        /// <param name="connectInfo">接続先データベースの情報</param> 
        /// <returns></returns> 
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
            catch (Exception)
            {
                throw;
            }
            return connection;
        }
        /// <summary> 
        /// データベースを参照します。 
        /// </summary> 
        /// <param name="sql">実行するSQL文</param> 
        /// <param name="connection">NpgsqlConnectionのインスタンス</param> 
        /// <returns>NpgsqlDataReader のインスタンス</returns> 
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
        /// <summary> 
        /// json 設定ファイルのデシリアライズ用クラス 
        /// </summary> 
        public class JsonElement
        {
            /// <summary> 
            /// 接続先データベースの情報 
            /// </summary> 
            /// <example> 
            /// Server=サーバー名; Port=ポート番号; User Id=ユーザーID; Password=パスワード; Database=データベース名; 
            /// </example> 
            public string DatabaseInfo { get; set; } = string.Empty;
        }
    }
}
