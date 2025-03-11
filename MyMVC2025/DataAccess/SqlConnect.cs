using Microsoft.Data.SqlClient;
using System;
using System.Data;

/*
Date Created: 2025.03.11
*/
namespace DataAccess
{
    public class SqlConnect
    {
        private readonly string _connectionString;

        // Constructor to initialize connection string
        public SqlConnect(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        // Method to open and return a SqlConnection
        public SqlConnection GetConnection()
        {
            try
            {
                SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                Console.WriteLine("Connection successful.");
                return connection;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"An error occurred while connecting to the database: {ex.Message}");
                throw;
            }
        }

        // Method to execute a SQL command (example: SELECT, INSERT, UPDATE, DELETE)
        public void ExecuteCommand(string query)
        {
            using (SqlConnection connection = GetConnection())
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // Method to execute a query and return a DataTable
        public DataTable ExecuteQuery(string query)
        {
            using (SqlConnection connection = GetConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    return dataTable;
                }
            }
        }

        // Optional: Method to close the connection explicitly if needed (normally handled by using statement)
        public void CloseConnection(SqlConnection connection)
        {
            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
                Console.WriteLine("Connection closed.");
            }
        }
    }
}
