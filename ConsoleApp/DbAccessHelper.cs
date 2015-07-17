using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class DbAccessHelper
    {
        public const string ConnStringInTestServier = "Data Source=192.168.200.29;Initial Catalog=aoh;User ID=sa;pwd=1qaz@WSX;";
        private readonly string connStr = "";
        public DbAccessHelper(string connString)
        {
            if (string.IsNullOrWhiteSpace(connString))
                throw new ArgumentNullException("connString");
            this.connStr = connString;
        }

        private SqlConnection GetOpenConnection()
        {
            if (!string.IsNullOrWhiteSpace(this.connStr))
            {
                var conn = new SqlConnection(this.connStr);
                conn.Open();
                return conn;
            }
            return null;
        }


        public void ExcuteNoQuery(string sql)
        {
            using (SqlConnection conn = new SqlConnection(this.connStr))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }
        public void ExcuteQuery(CommandType commandType, string sql)
        {
            using (SqlConnection conn = new SqlConnection(this.connStr))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandType = commandType;
                command.CommandText = sql;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                     
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write("[" + reader[0] + "],");
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
        public List<T> ExcuteQuery<T>(CommandType commandType, string sql) where T : class ,new()
        {
            using (SqlConnection conn = new SqlConnection(this.connStr))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandType = commandType;
                command.CommandText = sql;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    return Read<T>(reader);
                }
            }
        }
        private List<T> Read<T>(SqlDataReader reader) where T : class, new()
        {
            string[] fieldNameList = new string[reader.FieldCount];
            for (int i = 0; i < fieldNameList.Length; i++)
                fieldNameList[i] = reader.GetName(i);


            List<T> results = new List<T>();

            PropertyInfo[] properties = typeof(T).GetProperties();
            if (properties.Length > 0)
            {

                while (reader.Read())
                {

                    T o = Activator.CreateInstance<T>();
                    results.Add(o);

                    for (int j = 0; j < properties.Length; j++)
                    {
                        PropertyInfo p = properties[j];
                        string fieldName = "";
                        Array.ForEach(fieldNameList, field =>
                        {
                            if (field.Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                                fieldName = field;
                        });
                        if (!string.IsNullOrWhiteSpace(fieldName) && p.CanWrite && !DBNull.Value.Equals(reader[fieldName]))
                            p.SetValue(o, reader[fieldName], null);

                    }
                }
            }
            else
            {

                while (reader.Read())
                {
                    if (reader.FieldCount > 0 && !DBNull.Value.Equals(reader[0]))
                        results.Add((T)reader[0]);
                }

            }
            return results;
        }
        public void BulkCopy(DataTable dataTable, Type entity, string destTableName)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                return;

            string connString = connStr;

            PropertyInfo[] ps = entity.GetProperties();

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                SqlBulkCopy sbc = new SqlBulkCopy(conn);
                sbc.BatchSize = 100;
                sbc.DestinationTableName = destTableName ?? entity.Name;
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    string colName = dataTable.Columns[i].ColumnName;
                    if (colName != "PKID")
                        sbc.ColumnMappings.Add(colName, colName);
                }
                sbc.WriteToServer(dataTable);

            }
        }
    }
}
