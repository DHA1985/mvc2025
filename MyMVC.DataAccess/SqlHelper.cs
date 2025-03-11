using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Data.SqlClient;
using System.Security.Permissions;
using System.Xml;

public static class SqlHelper
{
  

    // Since this class provides only static methods, make the default constructor private to prevent 
    // instances from being created with "new SqlHelper()".
    private SqlHelper() { }

    // This method is used to attach an array of SqlParameters to a SqlCommand.
    // It assigns a value of DbNull to any parameter with a direction of InputOutput and a value of null.
    private static void AttachParameters(SqlCommand command, SqlParameter[] commandParameters)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        if (commandParameters != null)
        {
            foreach (var p in commandParameters)
            {
                if (p != null)
                {
                    // Check for derived output value with no value assigned
                    if ((p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Input) && p.Value == null)
                    {
                        p.Value = DBNull.Value;
                    }
                    command.Parameters.Add(p);
                }
            }
        }
    }

    // This method assigns dataRow column values to an array of SqlParameters.
    private static void AssignParameterValues(SqlParameter[] commandParameters, DataRow dataRow)
    {
        if (commandParameters == null || dataRow == null)
            return;

        int i = 0;
        foreach (var commandParameter in commandParameters)
        {
            if (commandParameter.ParameterName == null || commandParameter.ParameterName.Length <= 1)
            {
                throw new Exception($"Please provide a valid parameter name on the parameter #{i}, the ParameterName property has the following value: '{commandParameter.ParameterName}' .");
            }
            if (dataRow.Table.Columns.IndexOf(commandParameter.ParameterName.Substring(1)) != -1)
            {
                commandParameter.Value = dataRow[commandParameter.ParameterName.Substring(1)];
            }
            i++;
        }
    }

    // This method assigns an array of values to an array of SqlParameters.
    private static void AssignParameterValues(SqlParameter[] commandParameters, object[] parameterValues)
    {
        if (commandParameters == null && parameterValues == null) return;

        if (commandParameters.Length != parameterValues.Length)
        {
            throw new ArgumentException("Parameter count does not match Parameter Value count.");
        }

        for (int i = 0; i < commandParameters.Length; i++)
        {
            if (parameterValues[i] is IDbDataParameter paramInstance)
            {
                commandParameters[i].Value = paramInstance.Value ?? DBNull.Value;
            }
            else
            {
                commandParameters[i].Value = parameterValues[i] ?? DBNull.Value;
            }
        }
    }

    // This method opens (if necessary) and assigns a connection, transaction, command type, and parameters 
    // to the provided command.
    private static void PrepareCommand(SqlCommand command, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters, out bool mustCloseConnection)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (string.IsNullOrEmpty(commandText)) throw new ArgumentNullException(nameof(commandText));

        // If the provided connection is not open, we will open it
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
            mustCloseConnection = true;
        }
        else
        {
            mustCloseConnection = false;
        }

        // Associate the connection with the command
        command.Connection = connection;

        // Set the command text (stored procedure name or SQL statement)
        command.CommandText = commandText;

        // If we were provided a transaction, assign it
        if (transaction != null)
        {
            if (transaction.Connection == null)
                throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", nameof(transaction));

            command.Transaction = transaction;
        }

        // Set the command type
        command.CommandType = commandType;

        // Attach the command parameters if they are provided
        if (commandParameters != null)
        {
            AttachParameters(command, commandParameters);
        }
    }

   

    // Execute a SqlCommand (that returns no resultset and takes no parameters) against the database specified in
    // the connection string. 
    // e.g.:  
    // int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders")
    public static int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteNonQuery(connectionString, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns no resultset) against the database specified in the connection string
    // using the provided parameters.
    // e.g.:  
    // int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24))
    public static int ExecuteNonQuery(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            // Call the overload that takes a connection in place of the connection string
            return ExecuteNonQuery(connection, commandType, commandText, commandParameters);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns no resultset) against the database specified in 
    // the connection string using the provided parameter values.
    public static int ExecuteNonQuery(string connectionString, string spName, params object[] parameterValues)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException("spName");

        SqlParameter[] commandParameters;

        // If we receive parameter values, we need to figure out where they go
        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

            // Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            // Call the overload that takes an array of SqlParameters
            return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a SqlCommand (that returns no resultset and takes no parameters) against the provided SqlConnection. 
    public static int ExecuteNonQuery(SqlConnection connection, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteNonQuery(connection, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns no resultset) against the specified SqlConnection using the provided parameters.
    public static int ExecuteNonQuery(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (connection == null) throw new ArgumentNullException("connection");

        // Create a command and prepare it for execution
        SqlCommand cmd = new SqlCommand();
        int retval;
        bool mustCloseConnection = false;

        PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters, ref mustCloseConnection);

        // Finally, execute the command
        retval = cmd.ExecuteNonQuery();

        // Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear();

        if (mustCloseConnection) connection.Close();

        return retval;
    }

    // Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified SqlConnection
    // using the provided parameter values.
    public static int ExecuteNonQuery(SqlConnection connection, string spName, params object[] parameterValues)
    {
        if (connection == null) throw new ArgumentNullException("connection");
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException("spName");

        SqlParameter[] commandParameters;

        // If we receive parameter values, we need to figure out where they go
        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            // Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            // Call the overload that takes an array of SqlParameters
            return ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteNonQuery(connection, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a SqlCommand (that returns no resultset and takes no parameters) against the provided SqlTransaction.
    public static int ExecuteNonQuery(SqlTransaction transaction, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteNonQuery(transaction, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns no resultset) against the specified SqlTransaction using the provided parameters.
    public static int ExecuteNonQuery(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (transaction == null) throw new ArgumentNullException("transaction");
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");

        // Create a command and prepare it for execution
        SqlCommand cmd = new SqlCommand();
        int retval;
        bool mustCloseConnection = false;

        PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, ref mustCloseConnection);

        // Finally, execute the command
        retval = cmd.ExecuteNonQuery();

        // Detach the SqlParameters from the command object, so they can be used again
        cmd.Parameters.Clear();

        return retval;
    }

    // Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified SqlTransaction 
    // using the provided parameter values.
    public static int ExecuteNonQuery(SqlTransaction transaction, string spName, params object[] parameterValues)
    {
        if (transaction == null) throw new ArgumentNullException("transaction");
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", "transaction");
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException("spName");

        SqlParameter[] commandParameters;

        // If we receive parameter values, we need to figure out where they go
        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);

            // Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            // Call the overload that takes an array of SqlParameters
            return ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName);
        }
    }

  

    // Execute a SqlCommand (that returns a resultset and takes no parameters) against the database specified in
    // the connection string.
    // e.g.:
    // DataSet ds = SqlHelper.ExecuteDataset("", CommandType.StoredProcedure, "GetOrders");
    public static DataSet ExecuteDataset(string connectionString, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteDataset(connectionString, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns a resultset) against the database specified in the connection string
    // using the provided parameters.
    // e.g.:
    // DataSet ds = ExecuteDataset(connString, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    public static DataSet ExecuteDataset(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        // Create & open a SqlConnection, and dispose of it after we are done
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            // Call the overload that takes a connection in place of the connection string
            return ExecuteDataset(connection, commandType, commandText, commandParameters);
        }
    }

    // Execute a SqlCommand (that returns a resultset) against the provided SqlConnection.
    // e.g.:
    // DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders");
    public static DataSet ExecuteDataset(SqlConnection connection, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteDataset(connection, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns a resultset) against the specified SqlConnection using the provided parameters.
    // e.g.:
    // DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    public static DataSet ExecuteDataset(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));

        // Create a command and prepare it for execution
        SqlCommand cmd = new SqlCommand
        {
            CommandType = commandType,
            CommandText = commandText,
            Connection = connection
        };

        if (commandParameters != null)
            cmd.Parameters.AddRange(commandParameters);

        DataSet ds = new DataSet();
        SqlDataAdapter dataAdapter = new SqlDataAdapter(cmd);

        try
        {
            // Fill the DataSet using default values for DataTable names, etc
            dataAdapter.Fill(ds);
        }
        finally
        {
            dataAdapter.Dispose();
            cmd.Parameters.Clear();
        }

        return ds;
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in
    // the connection string using the provided parameter values.
    // This method will discover the parameters for the stored procedure, and assign the values based on parameter order.
    // e.g.:
    // DataSet ds = ExecuteDataset(connString, "GetOrders", 24, 36);
    public static DataSet ExecuteDataset(string connectionString, string spName, params object[] parameterValues)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        SqlParameter[] commandParameters = null;

        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

            // Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            // Call the overload that takes an array of SqlParameters
            return ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            // Otherwise we can just call the SP without params
            return ExecuteDataset(connectionString, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection
    // using the provided parameter values.
    // e.g.:
    // DataSet ds = ExecuteDataset(conn, "GetOrders", 24, 36);
    public static DataSet ExecuteDataset(SqlConnection connection, string spName, params object[] parameterValues)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        SqlParameter[] commandParameters = null;

        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            // Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            // Call the overload that takes an array of SqlParameters
            return ExecuteDataset(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            // Otherwise we can just call the SP without params
            return ExecuteDataset(connection, CommandType.StoredProcedure, spName);
        }
    }

   

    // Enum to indicate connection ownership
    private enum SqlConnectionOwnership
    {
        Internal,  // Connection is owned and managed by SqlHelper
        External   // Connection is owned and managed by the caller
    }

    public static SqlDataReader ExecuteReader(SqlConnection connection, SqlTransaction transaction,
        CommandType commandType, string commandText, SqlParameter[] commandParameters, SqlConnectionOwnership connectionOwnership)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));

        bool mustCloseConnection = false;
        SqlCommand cmd = new SqlCommand();

        try
        {
            SqlDataReader dataReader;

            PrepareCommand(cmd, connection, transaction, commandType, commandText, commandParameters, mustCloseConnection);

            bool mSuccess = false;
            int iTries = 0;

            do
            {
                try
                {
                    if (connectionOwnership == SqlConnectionOwnership.External)
                        dataReader = cmd.ExecuteReader();
                    else
                        dataReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                    mSuccess = true;
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException && ex.Message == "There is already an open DataReader associated with this Connection which must be closed first." && ex.Source == "System.Data")
                    {
                        iTries++;

                        if (iTries > 10)
                            throw;

                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (!mSuccess);

            bool canClear = true;
            foreach (SqlParameter commandParameter in cmd.Parameters)
            {
                if (commandParameter.Direction != ParameterDirection.Input)
                    canClear = false;
            }

            if (canClear)
                cmd.Parameters.Clear();

            return dataReader;
        }
        catch
        {
            if (mustCloseConnection) connection.Close();
            throw;
        }
    }

    public static SqlDataReader ExecuteReader(string connectionString, CommandType commandType, string commandText)
    {
        return ExecuteReader(connectionString, commandType, commandText, null);
    }

    public static SqlDataReader ExecuteReader(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        SqlConnection connection = new SqlConnection(connectionString);
        try
        {
            connection.Open();
            return ExecuteReader(connection, null, commandType, commandText, commandParameters, SqlConnectionOwnership.Internal);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public static SqlDataReader ExecuteReader(SqlConnection connection, CommandType commandType, string commandText)
    {
        return ExecuteReader(connection, commandType, commandText, null);
    }

    public static SqlDataReader ExecuteReader(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        return ExecuteReader(connection, null, commandType, commandText, commandParameters, SqlConnectionOwnership.External);
    }

    public static SqlDataReader ExecuteReader(SqlTransaction transaction, CommandType commandType, string commandText)
    {
        return ExecuteReader(transaction, commandType, commandText, null);
    }

    public static SqlDataReader ExecuteReader(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", nameof(transaction));

        return ExecuteReader(transaction.Connection, transaction, commandType, commandText, commandParameters, SqlConnectionOwnership.External);
    }

    public static SqlDataReader ExecuteReader(SqlTransaction transaction, string spName, params object[] parameterValues)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", nameof(transaction));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        SqlParameter[] commandParameters = null;

        if (parameterValues != null && parameterValues.Length > 0)
        {
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);
            AssignParameterValues(commandParameters, parameterValues);
            return ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteReader(transaction, CommandType.StoredProcedure, spName);
        }
    }
   

    // Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the database specified in 
    // the connection string. 
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount");
    // Parameters:
    // - connectionString - a valid connection string for a SqlConnection 
    // - commandType - the CommandType (stored procedure, text, etc.) 
    // - commandText - the stored procedure name or T-SQL command 
    // Returns: An object containing the value in the 1x1 resultset generated by the command
    public static object ExecuteScalar(string connectionString, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteScalar(connectionString, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns a 1x1 resultset) against the database specified in the connection string 
    // using the provided parameters.
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
    // Parameters:
    // - connectionString - a valid connection string for a SqlConnection 
    // - commandType - the CommandType (stored procedure, text, etc.) 
    // - commandText - the stored procedure name or T-SQL command 
    // - commandParameters - an array of SqlParameters used to execute the command 
    // Returns: An object containing the value in the 1x1 resultset generated by the command
    public static object ExecuteScalar(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // Call the overload that takes a connection in place of the connection string
            return ExecuteScalar(connection, commandType, commandText, commandParameters);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the database specified in 
    // the connection string using the provided parameter values. This method will discover the parameters for the 
    // stored procedure, and assign the values based on parameter order.
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(connString, "GetOrderCount", 24, 36);
    // Parameters:
    // - connectionString - a valid connection string for a SqlConnection 
    // - spName - the name of the stored procedure 
    // - parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    // Returns: An object containing the value in the 1x1 resultset generated by the command
    public static object ExecuteScalar(string connectionString, string spName, params object[] parameterValues)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        SqlParameter[] commandParameters;

        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

            // Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            // Call the overload that takes an array of SqlParameters
            return ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteScalar(connectionString, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the provided SqlConnection. 
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount");
    // Parameters:
    // - connection - a valid SqlConnection 
    // - commandType - the CommandType (stored procedure, text, etc.) 
    // - commandText - the stored procedure name or T-SQL command 
    // Returns: An object containing the value in the 1x1 resultset generated by the command 
    public static object ExecuteScalar(SqlConnection connection, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteScalar(connection, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    // using the provided parameters.
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
    // Parameters:
    // - connection - a valid SqlConnection 
    // - commandType - the CommandType (stored procedure, text, etc.) 
    // - commandText - the stored procedure name or T-SQL command 
    // - commandParameters - an array of SqlParameters used to execute the command 
    // Returns: An object containing the value in the 1x1 resultset generated by the command 
    public static object ExecuteScalar(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));

        using (var cmd = new SqlCommand())
        {
            bool mustCloseConnection = false;
            PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters, ref mustCloseConnection);

            var retval = cmd.ExecuteScalar();

            cmd.Parameters.Clear();

            if (mustCloseConnection)
                connection.Close();

            return retval;
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    // using the provided parameter values. This method will discover the parameters for the 
    // stored procedure, and assign the values based on parameter order.
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(conn, "GetOrderCount", 24, 36);
    // Parameters:
    // - connection - a valid SqlConnection 
    // - spName - the name of the stored procedure 
    // - parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    // Returns: An object containing the value in the 1x1 resultset generated by the command
    public static object ExecuteScalar(SqlConnection connection, string spName, params object[] parameterValues)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        SqlParameter[] commandParameters;

        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            // Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            // Call the overload that takes an array of SqlParameters
            return ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteScalar(connection, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the provided SqlTransaction.
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount");
    // Parameters:
    // - transaction - a valid SqlTransaction 
    // - commandType - the CommandType (stored procedure, text, etc.) 
    // - commandText - the stored procedure name or T-SQL command 
    // Returns: An object containing the value in the 1x1 resultset generated by the command 
    public static object ExecuteScalar(SqlTransaction transaction, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteScalar(transaction, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction
    // using the provided parameters.
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
    // Parameters:
    // - transaction - a valid SqlTransaction  
    // - commandType - the CommandType (stored procedure, text, etc.) 
    // - commandText - the stored procedure name or T-SQL command 
    // - commandParameters - an array of SqlParameters used to execute the command 
    // Returns: An object containing the value in the 1x1 resultset generated by the command 
    public static object ExecuteScalar(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", nameof(transaction));

        using (var cmd = new SqlCommand())
        {
            bool mustCloseConnection = false;
            PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, ref mustCloseConnection);

            var retval = cmd.ExecuteScalar();

            cmd.Parameters.Clear();

            return retval;
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction 
    // using the provided parameter values. This method will discover the parameters for the 
    // stored procedure, and assign the values based on parameter order.
    // e.g.:  
    // var orderCount = (int)ExecuteScalar(trans, "GetOrderCount", 24, 36);
    // Parameters:
    // - transaction - a valid SqlTransaction 
    // - spName - the name of the stored procedure 
    // - parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    // Returns: An object containing the value in the 1x1 resultset generated by the command
    public static object ExecuteScalar(SqlTransaction transaction, string spName, params object[] parameterValues)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", nameof(transaction));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        SqlParameter[] commandParameters;

        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);

            // Assign the provided values to these parameters based on parameter order
            AssignParameterValues(commandParameters, parameterValues);

            // Call the overload that takes an array of SqlParameters
            return ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteScalar(transaction, CommandType.StoredProcedure, spName);
        }
    }



    // Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection. 
    // e.g.:  
    // XmlReader r = ExecuteXmlReader(conn, CommandType.StoredProcedure, "GetOrders");
    // Parameters:
    // -connection - a valid SqlConnection 
    // -commandType - the CommandType (stored procedure, text, etc.) 
    // -commandText - the stored procedure name or T-SQL command using "FOR XML AUTO" 
    // Returns: An XmlReader containing the resultset generated by the command 
    public static XmlReader ExecuteXmlReader(SqlConnection connection, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteXmlReader(connection, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns a resultset) against the specified SqlConnection 
    // using the provided parameters.
    // e.g.:  
    // XmlReader r = ExecuteXmlReader(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    // Parameters:
    // -connection - a valid SqlConnection 
    // -commandType - the CommandType (stored procedure, text, etc.) 
    // -commandText - the stored procedure name or T-SQL command using "FOR XML AUTO" 
    // -commandParameters - an array of SqlParamters used to execute the command 
    // Returns: An XmlReader containing the resultset generated by the command 
    public static XmlReader ExecuteXmlReader(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        SqlCommand cmd = new SqlCommand();
        bool mustCloseConnection = false;

        try
        {
            XmlReader retval;

            // Assuming PrepareCommand is implemented elsewhere
            PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters, mustCloseConnection);

            // Execute the command and get the result
            retval = cmd.ExecuteXmlReader();

            // Detach the SqlParameters from the command object so they can be reused
            cmd.Parameters.Clear();

            return retval;
        }
        catch
        {
            if (mustCloseConnection)
                connection.Close();
            throw;
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    // using the provided parameter values. This method will discover the parameters for the 
    // stored procedure, and assign the values based on parameter order.
    // This method provides no access to output parameters or the stored procedure's return value parameter.
    // e.g.:  
    // XmlReader r = ExecuteXmlReader(conn, "GetOrders", 24, 36);
    // Parameters:
    // -connection - a valid SqlConnection 
    // -spName - the name of the stored procedure using "FOR XML AUTO" 
    // -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    // Returns: An XmlReader containing the resultset generated by the command 
    public static XmlReader ExecuteXmlReader(SqlConnection connection, string spName, params object[] parameterValues)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        if (string.IsNullOrEmpty(spName))
            throw new ArgumentNullException(nameof(spName));

        SqlParameter[] commandParameters = null;

        // If we receive parameter values, we need to figure out where they go
        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Assuming GetSpParameterSet and AssignParameterValues are implemented elsewhere
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            AssignParameterValues(commandParameters, parameterValues);

            return ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteXmlReader(connection, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction
    // e.g.:  
    // XmlReader r = ExecuteXmlReader(trans, CommandType.StoredProcedure, "GetOrders");
    // Parameters:
    // -transaction - a valid SqlTransaction
    // -commandType - the CommandType (stored procedure, text, etc.) 
    // -commandText - the stored procedure name or T-SQL command using "FOR XML AUTO" 
    // Returns: An XmlReader containing the resultset generated by the command 
    public static XmlReader ExecuteXmlReader(SqlTransaction transaction, CommandType commandType, string commandText)
    {
        // Pass through the call providing null for the set of SqlParameters
        return ExecuteXmlReader(transaction, commandType, commandText, null);
    }

    // Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
    // using the provided parameters.
    // e.g.:  
    // XmlReader r = ExecuteXmlReader(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
    // Parameters:
    // -transaction - a valid SqlTransaction
    // -commandType - the CommandType (stored procedure, text, etc.) 
    // -commandText - the stored procedure name or T-SQL command using "FOR XML AUTO" 
    // -commandParameters - an array of SqlParamters used to execute the command 
    // Returns: An XmlReader containing the resultset generated by the command
    public static XmlReader ExecuteXmlReader(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        if (transaction.Connection == null)
            throw new ArgumentException("The transaction was rolled back or committed, please provide an open transaction.", nameof(transaction));

        SqlCommand cmd = new SqlCommand();
        bool mustCloseConnection = false;

        // Assuming PrepareCommand is implemented elsewhere
        PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters, mustCloseConnection);

        // Execute the command and get the result
        XmlReader retval = cmd.ExecuteXmlReader();

        // Detach the SqlParameters from the command object so they can be reused
        cmd.Parameters.Clear();

        return retval;
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    // using the provided parameter values. This method will discover the parameters for the 
    // stored procedure, and assign the values based on parameter order.
    // This method provides no access to output parameters or the stored procedure's return value parameter.
    // e.g.:  
    // XmlReader r = ExecuteXmlReader(trans, "GetOrders", 24, 36);
    // Parameters:
    // -transaction - a valid SqlTransaction
    // -spName - the name of the stored procedure 
    // -parameterValues - an array of objects to be assigned as the input values of the stored procedure 
    // Returns: A dataset containing the resultset generated by the command
    public static XmlReader ExecuteXmlReader(SqlTransaction transaction, string spName, params object[] parameterValues)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        if (transaction.Connection == null)
            throw new ArgumentException("The transaction was rolled back or committed, please provide an open transaction.", nameof(transaction));

        if (string.IsNullOrEmpty(spName))
            throw new ArgumentNullException(nameof(spName));

        SqlParameter[] commandParameters = null;

        // If we receive parameter values, we need to figure out where they go
        if (parameterValues != null && parameterValues.Length > 0)
        {
            // Assuming GetSpParameterSet and AssignParameterValues are implemented elsewhere
            commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);

            AssignParameterValues(commandParameters, parameterValues);

            return ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName);
        }
    }



    public static void FillDataset(string connectionString, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            FillDataset(connection, commandType, commandText, dataSet, tableNames);
        }
    }

    public static void FillDataset(string connectionString, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params SqlParameter[] commandParameters)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            FillDataset(connection, commandType, commandText, dataSet, tableNames, commandParameters);
        }
    }

    public static void FillDataset(string connectionString, string spName, DataSet dataSet, string[] tableNames, params object[] parameterValues)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            FillDataset(connection, spName, dataSet, tableNames, parameterValues);
        }
    }

    public static void FillDataset(SqlConnection connection, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
    {
        FillDataset(connection, commandType, commandText, dataSet, tableNames, null);
    }

    public static void FillDataset(SqlConnection connection, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params SqlParameter[] commandParameters)
    {
        FillDataset(connection, null, commandType, commandText, dataSet, tableNames, commandParameters);
    }

    public static void FillDataset(SqlConnection connection, string spName, DataSet dataSet, string[] tableNames, params object[] parameterValues)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        if (parameterValues != null && parameterValues.Length > 0)
        {
            var commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);
            AssignParameterValues(commandParameters, parameterValues);
            FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
        }
        else
        {
            FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames);
        }
    }

    public static void FillDataset(SqlTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
    {
        FillDataset(transaction, commandType, commandText, dataSet, tableNames, null);
    }

    public static void FillDataset(SqlTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params SqlParameter[] commandParameters)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rolled back or committed, please provide an open transaction.", nameof(transaction));
        FillDataset(transaction.Connection, transaction, commandType, commandText, dataSet, tableNames, commandParameters);
    }

    public static void FillDataset(SqlTransaction transaction, string spName, DataSet dataSet, string[] tableNames, params object[] parameterValues)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rolled back or committed, please provide an open transaction.", nameof(transaction));
        if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        if (parameterValues != null && parameterValues.Length > 0)
        {
            var commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);
            AssignParameterValues(commandParameters, parameterValues);
            FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
        }
        else
        {
            FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames);
        }
    }

    private static void FillDataset(SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames, params SqlParameter[] commandParameters)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));

        var command = new SqlCommand();
        bool mustCloseConnection = false;
        PrepareCommand(command, connection, transaction, commandType, commandText, commandParameters, out mustCloseConnection);

        var dataAdapter = new SqlDataAdapter(command);

        try
        {
            if (tableNames != null && tableNames.Length > 0)
            {
                string tableName = "Table";
                for (int i = 0; i < tableNames.Length; i++)
                {
                    if (string.IsNullOrEmpty(tableNames[i])) throw new ArgumentException("The tableNames parameter must contain a list of tables, a value was provided as null or empty string.", nameof(tableNames));
                    dataAdapter.TableMappings.Add(tableName, tableNames[i]);
                    tableName = tableName + (i + 1).ToString();
                }
            }

            dataAdapter.Fill(dataSet);
            command.Parameters.Clear();
        }
        finally
        {
            dataAdapter.Dispose();
        }

        if (mustCloseConnection) connection.Close();
    }

    private static void PrepareCommand(SqlCommand command, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters, out bool mustCloseConnection)
    {
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
            mustCloseConnection = true;
        }
        else
        {
            mustCloseConnection = false;
        }

        command.Connection = connection;
        command.CommandText = commandText;
        command.CommandType = commandType;

        if (transaction != null)
        {
            command.Transaction = transaction;
        }

        if (commandParameters != null)
        {
            foreach (var param in commandParameters)
            {
                command.Parameters.Add(param);
            }
        }
    }

    private static void AssignParameterValues(SqlParameter[] commandParameters, object[] parameterValues)
    {
        if (commandParameters == null || parameterValues == null) return;

        for (int i = 0; i < commandParameters.Length; i++)
        {
            commandParameters[i].Value = parameterValues[i];
        }
    }



    public static void UpdateDataset(SqlCommand insertCommand, SqlCommand deleteCommand, SqlCommand updateCommand, DataSet dataSet, string tableName)
    {
        if (insertCommand == null) throw new ArgumentNullException(nameof(insertCommand));
        if (deleteCommand == null) throw new ArgumentNullException(nameof(deleteCommand));
        if (updateCommand == null) throw new ArgumentNullException(nameof(updateCommand));
        if (dataSet == null) throw new ArgumentNullException(nameof(dataSet));
        if (string.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));

        using (var dataAdapter = new SqlDataAdapter())
        {
            dataAdapter.UpdateCommand = updateCommand;
            dataAdapter.InsertCommand = insertCommand;
            dataAdapter.DeleteCommand = deleteCommand;

            dataAdapter.Update(dataSet, tableName);
            dataSet.AcceptChanges();
        }
    }

    public static SqlCommand CreateCommand(SqlConnection connection, string spName, params string[] sourceColumns)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));

        var cmd = new SqlCommand(spName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (sourceColumns != null && sourceColumns.Length > 0)
        {
            var commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            for (int i = 0; i < sourceColumns.Length; i++)
            {
                commandParameters[i].SourceColumn = sourceColumns[i];
            }

            AttachParameters(cmd, commandParameters);
        }

        return cmd;
    }

    public static int ExecuteNonQueryTypedParams(string connectionString, string spName, DataRow dataRow)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            var commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName);
        }
    }

    public static int ExecuteNonQueryTypedParams(SqlConnection connection, string spName, DataRow dataRow)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            var commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteNonQuery(connection, CommandType.StoredProcedure, spName);
        }
    }

    public static int ExecuteNonQueryTypedParams(SqlTransaction transaction, string spName, DataRow dataRow)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rolled back or committed. Please provide an open transaction.", nameof(transaction));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            var commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName);
        }
    }

    // Placeholder methods for missing logic
    private static void AttachParameters(SqlCommand command, SqlParameter[] parameters) { /* Implementation here */ }
    private static void AssignParameterValues(SqlParameter[] parameters, DataRow dataRow) { /* Implementation here */ }
    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in
    // the connection string using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    // Parameters:
    // - connectionString: A valid connection string for a SqlConnection
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns:
    // a dataset containing the resultset generated by the command
    public static DataSet ExecuteDatasetTypedParams(string connectionString, string spName, DataRow dataRow)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    // using the dataRow column values as the store procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    // Parameters:
    // - connection: A valid SqlConnection object
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns:
    // a dataset containing the resultset generated by the command
    public static DataSet ExecuteDatasetTypedParams(SqlConnection connection, string spName, DataRow dataRow)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteDataset(connection, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    // using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on row values.
    // Parameters:
    // - transaction: A valid SqlTransaction object
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns:
    // a dataset containing the resultset generated by the command
    public static DataSet ExecuteDatasetTypedParams(SqlTransaction transaction, string spName, DataRow dataRow)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rollbacked or committed, please provide an open transaction.", nameof(transaction));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteDataset(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteDataset(transaction, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in
    // the connection string using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    // Parameters:
    // - connectionString: A valid connection string for a SqlConnection
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns:
    // a SqlDataReader containing the resultset generated by the command
    public static SqlDataReader ExecuteReaderTypedParams(string connectionString, string spName, DataRow dataRow)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteReader(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteReader(connectionString, CommandType.StoredProcedure, spName);
        }
    }

    /*From chatGPT:
    Key Changes in C#:
    Method Signatures: ByVal is replaced with proper parameter types (string, SqlConnection, DataRow, SqlTransaction).
    Null Checks: Used string.IsNullOrEmpty for string checks and if (dataRow != null) for null checks on objects.
    Return Type: The method now returns a SqlDataReader which is the C# equivalent of VB.NET's return type.
    Method Calls: Replaced VB.NET's SqlHelper.ExecuteReader and other similar calls with C# equivalents.
    This should work well for .NET Core, as long as the SqlHelper and SqlHelperParameterCache methods are adapted for .NET Core. Let me know if you need further modifications!
    */

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    // using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    // Parameters:
    // - connection: A valid SqlConnection object
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns:
    // a SqlDataReader containing the resultset generated by the command
    public static SqlDataReader ExecuteReaderTypedParams(SqlConnection connection, string spName, DataRow dataRow)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteReader(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteReader(connection, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    // using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    // Parameters:
    // - transaction: A valid SqlTransaction object
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns:
    // a SqlDataReader containing the resultset generated by the command
    public static SqlDataReader ExecuteReaderTypedParams(SqlTransaction transaction, string spName, DataRow dataRow)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rolled back or committed, please provide an open transaction.", nameof(transaction));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteReader(transaction, CommandType.StoredProcedure, spName);
        }
    }


    /* Date: 2025.03.11 - Convert VB.net to C#
    Key Changes in C#:
    Method Signatures: ByVal is replaced with proper parameter types (string, SqlConnection, DataRow, SqlTransaction).
    Null Checks: Used string.IsNullOrEmpty for string checks and if (dataRow != null) for null checks on objects.
    Return Type: The method now returns an object which is the C# equivalent of VB.NET's return type.
    Method Calls: Replaced VB.NET's SqlHelper.ExecuteScalar and other similar calls with C# equivalents.

    */
    // Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the database specified in
    // the connection string using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    // Parameters:
    // - connectionString: A valid connection string for a SqlConnection
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns:
    // An object containing the value in the 1x1 resultset generated by the command
    public static object ExecuteScalarTypedParams(string connectionString, string spName, DataRow dataRow)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connectionString, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteScalar(connectionString, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection 
    // using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    // Parameters:
    // - connection: A valid SqlConnection object
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns: 
    // an object containing the value in the 1x1 resultset generated by the command
    public static object ExecuteScalarTypedParams(SqlConnection connection, string spName, DataRow dataRow)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteScalar(connection, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction
    // using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    // Parameters:
    // - transaction: A valid SqlTransaction object
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns: 
    // an object containing the value in the 1x1 resultset generated by the command
    public static object ExecuteScalarTypedParams(SqlTransaction transaction, string spName, DataRow dataRow)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rolled back or committed, please provide an open transaction.", nameof(transaction));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteScalar(transaction, CommandType.StoredProcedure, spName);
        }
    }
    /*
    Key Changes in C#:
    Method Signatures: The ByVal keyword is replaced with proper parameter types (string, SqlConnection, DataRow, SqlTransaction).
    Null Checks: Used string.IsNullOrEmpty for string checks and if (dataRow != null) for object null checks.
    Return Type: The method now returns an XmlReader which is the C# equivalent of the VB.NET return type.
    Method Calls: Replaced VB.NET's SqlHelper.ExecuteXmlReader and other similar calls with their C# equivalents.
    */
    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection 
    // using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    // Parameters:
    // - connection: A valid SqlConnection object
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns: 
    // an XmlReader containing the resultset generated by the command
    public static XmlReader ExecuteXmlReaderTypedParams(SqlConnection connection, string spName, DataRow dataRow)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(connection, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteXmlReader(connection, CommandType.StoredProcedure, spName);
        }
    }

    // Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction 
    // using the dataRow column values as the stored procedure's parameters values.
    // This method will query the database to discover the parameters for the 
    // stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
    // Parameters:
    // - transaction: A valid SqlTransaction object
    // - spName: the name of the stored procedure
    // - dataRow: The dataRow used to hold the stored procedure's parameter values.
    // Returns: 
    // an XmlReader containing the resultset generated by the command
    public static XmlReader ExecuteXmlReaderTypedParams(SqlTransaction transaction, string spName, DataRow dataRow)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (transaction.Connection == null) throw new ArgumentException("The transaction was rolled back or committed, please provide an open transaction.", nameof(transaction));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        // If the row has values, the store procedure parameters must be initialized
        if (dataRow != null && dataRow.ItemArray.Length > 0)
        {
            // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
            SqlParameter[] commandParameters = SqlHelperParameterCache.GetSpParameterSet(transaction.Connection, spName);

            // Set the parameters values
            AssignParameterValues(commandParameters, dataRow);

            return SqlHelper.ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters);
        }
        else
        {
            return SqlHelper.ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName);
        }
    }
}


//End Class SqlHelper
/*  
Date: 2025.03.11
Converted from VB.net to C# by chatgpt:
Key Changes:
Hashtable to Dictionary<string, SqlParameter[]>: The Hashtable is replaced with Dictionary<string, SqlParameter[]> in C#, as it is type-safe and more modern.
Method Signature and Return Types: Adjusted method signatures to follow C# syntax.
IIf replaced with ternary operator: C# uses a ternary operator (condition ? true_value : false_value) instead of the IIf function in VB.NET.
Handling SQL Connection and Parameter Discovery: Corrected some VB.NET-specific logic to ensure proper handling of SQL connections and parameter retrieval in C#.
This conversion should work seamlessly with .NET Core. Let me know if you need further adjustments!
*/

public static class SqlHelperParameterCache
{
   

    // Since this class provides only static methods, make the default constructor private to prevent 
    // instances from being created with "new SqlHelperParameterCache()".
    private SqlHelperParameterCache() { }

    private static readonly Dictionary<string, SqlParameter[]> paramCache = new Dictionary<string, SqlParameter[]>();

    // Resolve at runtime the appropriate set of SqlParameters for a stored procedure
    // Parameters:
    // - connection: a valid SqlConnection
    // - spName: the name of the stored procedure
    // - includeReturnValueParameter: whether or not to include their return value parameter
    // Returns: SqlParameter[]
    private static SqlParameter[] DiscoverSpParameterSet(SqlConnection connection, string spName, bool includeReturnValueParameter, params object[] parameterValues)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (string.IsNullOrEmpty(spName)) throw new ArgumentNullException(nameof(spName));

        var cmd = new SqlCommand(spName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        SqlParameter[] discoveredParameters;

        connection.Open();
        SqlCommandBuilder.DeriveParameters(cmd);
        connection.Close();

        if (!includeReturnValueParameter && cmd.Parameters[0].Direction == ParameterDirection.ReturnValue)
        {
            cmd.Parameters.RemoveAt(0);
        }

        discoveredParameters = new SqlParameter[cmd.Parameters.Count - 1];
        cmd.Parameters.CopyTo(discoveredParameters, 0);

        // Initialize the parameters with a DBNull value
        foreach (var discoveredParameter in discoveredParameters)
        {
            discoveredParameter.Value = DBNull.Value;
        }

        return discoveredParameters;
    }

    // Deep copy of cached SqlParameter array
    private static SqlParameter[] CloneParameters(SqlParameter[] originalParameters)
    {
        var clonedParameters = new SqlParameter[originalParameters.Length];
        for (int i = 0; i < originalParameters.Length; i++)
        {
            clonedParameters[i] = (SqlParameter)((ICloneable)originalParameters[i]).Clone();
        }
        return clonedParameters;
    }

   

   

    // Add parameter array to the cache
    // Parameters:
    // - connectionString: a valid connection string for a SqlConnection
    // - commandText: the stored procedure name or T-SQL command
    // - commandParameters: an array of SqlParameters to be cached
    public static void CacheParameterSet(string connectionString, string commandText, params SqlParameter[] commandParameters)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(commandText)) throw new ArgumentNullException(nameof(commandText));

        string hashKey = connectionString + ":" + commandText;

        paramCache[hashKey] = commandParameters;
    }

    // Retrieve a parameter array from the cache
    // Parameters:
    // - connectionString: a valid connection string for a SqlConnection
    // - commandText: the stored procedure name or T-SQL command
    // Returns: An array of SqlParameters
    public static SqlParameter[] GetCachedParameterSet(string connectionString, string commandText)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
        if (string.IsNullOrEmpty(commandText)) throw new ArgumentNullException(nameof(commandText));

        string hashKey = connectionString + ":" + commandText;
        if (paramCache.TryGetValue(hashKey, out var cachedParameters))
        {
            return CloneParameters(cachedParameters);
        }

        return null;
    }



   

    // Retrieves the set of SqlParameters appropriate for the stored procedure.
    // This method will query the database for this information, and then store it in a cache for future requests.
    // Parameters:
    // - connectionString: a valid connection string for a SqlConnection
    // - spName: the name of the stored procedure
    // Returns: An array of SqlParameters
    public static SqlParameter[] GetSpParameterSet(string connectionString, string spName)
    {
        return GetSpParameterSet(connectionString, spName, false);
    }

    // Retrieves the set of SqlParameters appropriate for the stored procedure.
    // This method will query the database for this information, and then store it in a cache for future requests.
    // Parameters:
    // - connectionString: a valid connection string for a SqlConnection
    // - spName: the name of the stored procedure
    // - includeReturnValueParameter: a bool value indicating whether the return value parameter should be included in the results
    // Returns: An array of SqlParameters
    public static SqlParameter[] GetSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

        using (var connection = new SqlConnection(connectionString))
        {
            return GetSpParameterSetInternal(connection, spName, includeReturnValueParameter);
        }
    }

    // Retrieves the set of SqlParameters appropriate for the stored procedure.
    // This method will query the database for this information, and then store it in a cache for future requests.
    // Parameters:
    // - connection: a valid SqlConnection object
    // - spName: the name of the stored procedure
    // - includeReturnValueParameter: a bool value indicating whether the return value parameter should be included in the results
    // Returns: An array of SqlParameters
    public static SqlParameter[] GetSpParameterSet(SqlConnection connection, string spName)
    {
        return GetSpParameterSet(connection, spName, false);
    }

    // Retrieves the set of SqlParameters appropriate for the stored procedure.
    // This method will query the database for this information, and then store it in a cache for future requests.
    // Parameters:
    // - connection: a valid SqlConnection object
    // - spName: the name of the stored procedure
    // - includeReturnValueParameter: a bool value indicating whether the return value parameter should be included in the results
    // Returns: An array of SqlParameters
    public static SqlParameter[] GetSpParameterSet(SqlConnection connection, string spName, bool includeReturnValueParameter)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));

        using (var clonedConnection = (SqlConnection)((ICloneable)connection).Clone())
        {
            return GetSpParameterSetInternal(clonedConnection, spName, includeReturnValueParameter);
        }
    }

    // Retrieves the set of SqlParameters appropriate for the stored procedure.
    // This method will query the database for this information, and then store it in a cache for future requests.
    // Parameters:
    // - connection: a valid SqlConnection object
    // - spName: the name of the stored procedure
    // - includeReturnValueParameter: a bool value indicating whether the return value parameter should be included in the results
    // Returns: An array of SqlParameters
    private static SqlParameter[] GetSpParameterSetInternal(SqlConnection connection, string spName, bool includeReturnValueParameter)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));

        string hashKey = connection.ConnectionString + ":" + spName + (includeReturnValueParameter ? ":include ReturnValue Parameter" : string.Empty);

        if (paramCache.TryGetValue(hashKey, out var cachedParameters))
        {
            return CloneParameters(cachedParameters);
        }

        var spParameters = DiscoverSpParameterSet(connection, spName, includeReturnValueParameter);
        paramCache[hashKey] = spParameters;

        return spParameters;
    }

   
}
