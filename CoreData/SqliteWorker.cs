using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CoreData
{
    public static class SqiliteQueries
    {
        public static readonly string UpdateQuery = @"Update node Set low = @low, high = @high where id = @id";
        public static readonly string SelectQuery = @"Select * from node";
        public static readonly string DbName = @"Data Source=/spectrum.db";
    }

    public static class SqliteWorker
    {
        public static List<NodeData> LoadFromDb()
        {
            var list = new List<NodeData>();
            using (var conn = new SQLiteConnection(SqiliteQueries.DbName))
            {
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = SqiliteQueries.SelectQuery;
                    
                    SQLiteDataReader rd = cmd.ExecuteReader();

                    while (rd.Read())
                    {
                        list.Add(new NodeData(rd.GetInt32(0), rd.GetDouble(1), rd.GetDouble(2),
                            (DeseaseType) rd.GetInt32(3), (ParamType) rd.GetInt32(4)));
                    }
                }
            }
            return list;
        }

        public static bool SaveToDb(List<NodeData> list)
        {
            foreach (var node in list)
            {
                using (var conn = new SQLiteConnection(SqiliteQueries.DbName))
                {
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = SqiliteQueries.UpdateQuery;
                        cmd.Parameters.AddWithValue("@low", node.Low);
                        cmd.Parameters.AddWithValue("@high", node.High);
                        cmd.Parameters.AddWithValue("@id", node.Id);
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return true;
        }

        public static List<NodeData> LoadFromJson()
        {
            string path = @"data.json";
            var list = new List<NodeData>();
            if (File.Exists(path))
            {
                string readText = File.ReadAllText(path);
                list = JsonConvert.DeserializeObject<List<NodeData>>(readText);
            }

            if (list.Count < 9)
            {
                int cnt = 0;
                list.Clear();
                for (var i = 1; i <= 3; ++i)
                {
                    for (var j = 1; j <= 3; ++j)
                    {
                        cnt++;
                        list.Add(new NodeData(cnt, double.MaxValue, double.MinValue, (DeseaseType)i, (ParamType)j));
                    }
                }
            }
            return list;
        }

        public static bool SaveToJson(List<NodeData> list)
        {
            
            var json = JsonConvert.SerializeObject(list);

            string path = @"data.json";
            File.WriteAllText(path, json);
            return true;
        }

    }
}
