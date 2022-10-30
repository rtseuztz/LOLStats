using System;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Data;
using System.Data.SqlClient;
namespace sqltest
{

    class SQL
    {
        public static string connectionString = Environment.GetEnvironmentVariable("AzureSqlConn") ?? "";
        public static async Task<DataTable> executeQuery(string query)
        {
            return await executeQuery(query, getParams(new dynamic[] { }));
        }
        public static async Task<DataTable> executeQuery(string query, (string key, string value)[] parameters)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            command.Parameters.AddWithValue($"@{parameters[i].key}", parameters[i].value);
                        }
                        DataTable dataTable = new DataTable();
                        SqlDataAdapter da = new SqlDataAdapter(command);
                        da.Fill(dataTable);
                        return dataTable;
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
                return new DataTable();
            }
        }
        public static (string key, string value)[] getParams(dynamic[] p)
        {
            if (p.Length % 2 != 0)
            {
                throw new Exception("Invalid number of parameters");
            }
            (string key, string value)[] parameters = new (string key, string value)[p.Length / 2];
            for (int i = 0; i < p.Length; i += 2)
            {
                try
                {
                    p[i + 1] = Convert.ToString(p[i + 1]);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                parameters[i / 2] = ((string)p[i], p[i + 1]);
            }
            return parameters;
        }
    }
}