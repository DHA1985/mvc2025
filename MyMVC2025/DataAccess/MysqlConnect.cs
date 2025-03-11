using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace DataAccess
{
    public class MySqlConnect
    {
        private readonly string _connectionString;

        // Constructor to set connection string
        public MySqlConnect(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Method to open the connection
        public MySqlConnection GetConnection()
        {
            MySqlConnection connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        // Method to execute SELECT query
        public DataTable ExecuteSelectQuery(string query)
        {
            using (MySqlConnection connection = GetConnection())
            {
                MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                DataTable resultTable = new DataTable();
                adapter.Fill(resultTable);
                return resultTable;
            }
        }

        // Method to execute non-query (INSERT, UPDATE, DELETE)
        public int ExecuteNonQuery(string query)
        {
            using (MySqlConnection connection = GetConnection())
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                return command.ExecuteNonQuery();
            }
        }

        // Method to execute query and return a single value (scalar query)
        public object ExecuteScalar(string query)
        {
            using (MySqlConnection connection = GetConnection())
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                return command.ExecuteScalar();
            }
        }
    }
}
