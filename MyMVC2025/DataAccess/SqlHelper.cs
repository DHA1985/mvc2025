using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace DataAccess
{
    public class SqlHelper
    {
        private readonly SqlConnect _sqlConnect;

        // Constructor to initialize SqlConnect class
        public SqlHelper(string connectionString)
        {
            _sqlConnect = new SqlConnect(connectionString);
        }

        // Method to execute a stored procedure for Select queries (returns a DataTable)
        public DataTable ExecuteSelectProcedure(string storedProcedureName, params SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection connection = _sqlConnect.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(parameters);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable resultTable = new DataTable();
                            adapter.Fill(resultTable);
                            return resultTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing SELECT procedure: {ex.Message}");
                throw;
            }
        }

        // Method to execute a stored procedure for Insert, Update, Delete queries
        public int ExecuteNonQueryProcedure(string storedProcedureName, params SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection connection = _sqlConnect.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(parameters);

                        return command.ExecuteNonQuery(); // Returns the number of rows affected
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing non-query procedure: {ex.Message}");
                throw;
            }
        }

        // Method to execute a stored procedure with an output parameter (e.g., Insert with output ID)
        public object ExecuteScalarProcedure(string storedProcedureName, params SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection connection = _sqlConnect.GetConnection())
                {
                    using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddRange(parameters);

                        return command.ExecuteScalar(); // Returns the value of the first column of the first row
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing scalar procedure: {ex.Message}");
                throw;
            }
        }
    }
}
