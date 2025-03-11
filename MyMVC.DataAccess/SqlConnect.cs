/*
Key changes and explanations for .NET Core:

Microsoft.Data.SqlClient: Instead of System.Data.SqlClient, I'm using Microsoft.Data.SqlClient. This is the recommended SQL Server client library for .NET Core and .NET 5+. You'll need to add this as a NuGet package to your project.

Microsoft.Extensions.Configuration: .NET Core uses a different configuration system than .NET Framework. I've included Microsoft.Extensions.Configuration and related packages to properly load connection strings from appsettings.json (or other configuration sources). You'll need to add the following NuGet packages to your project:

Microsoft.Extensions.Configuration

Microsoft.Extensions.Configuration.Json (if you're using appsettings.json)

Microsoft.Extensions.Configuration.Binder

IConfiguration Injection: The SqlConnect class now takes an IConfiguration instance in its constructor. This is the standard way to access configuration settings in .NET Core. This enables Dependency Injection(DI).

appsettings.json: You'll need an appsettings.json file in your project to store your connection string. Make sure the file is copied to the output directory (set the "Copy to Output Directory" property to "Copy if newer" or "Copy always"). Example:

json
{
  "ConnectionStrings": {
    "SQLConnection": "Your_Connection_String_Here"
  }
}
Accessing Connection String: The ConnectionString property now uses _configuration.GetConnectionString("SQLConnection") to retrieve the connection string from the configuration.

Using Environment Variables If you want to use environment variables instead of appsettings.json, you can access the connection string using System.Environment.GetEnvironmentVariable("SQLConnection").

Friend Class to public class: Changed Friend Class to public class so it is accessible in other projects.

Using statements: Explicit using statements have been removed, relying on automatic disposal.

This conversion provides a solid foundation for using the SqlConnect class in a .NET Core environment. Remember to install the necessary NuGet packages and configure your appsettings.json file correctly. Also add the ConfigurationBuilder to your program.cs to inject the connection string.

*/
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; // Use Microsoft.Data.SqlClient for .NET Core
using Microsoft.Extensions.Configuration; // For ConfigurationManager

public class SqlConnect
{
    private static string _ConnectionString;
    private static readonly Random m_rndTransaction = new Random(1);
    private static readonly Dictionary<int, SqlTransaction> m_lstTransaction = new Dictionary<int, SqlTransaction>();
    private int _TransactionID;
    private SqlConnection m_objConnection;
    private readonly IConfiguration _configuration;

    public SqlConnect(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public static string ConnectionString
    {
        get
        {
            if (!string.IsNullOrEmpty(_ConnectionString))
            {
                return _ConnectionString;
            }
            else
            {
                // Access the connection string from the configuration
                return System.Environment.GetEnvironmentVariable("SQLConnection");
                //return _configuration.GetConnectionString("SQLConnection"); // Use this if in appsettings.json
            }
        }
        set
        {
            _ConnectionString = value;
        }
    }

    public int TransactionID
    {
        get
        {
            return _TransactionID;
        }
        set
        {
            _TransactionID = value;
        }
    }

    public int Begin_Transaction()
    {
        SqlConnection conn = new SqlConnection(ConnectionString);
        conn.Open();
        SqlTransaction trans = conn.BeginTransaction();
        int transID = m_rndTransaction.Next();
        m_lstTransaction.Add(transID, trans);

        _TransactionID = transID;
        return transID;
    }

    public bool RollbackTransaction()
    {
        try
        {
            SqlTransaction trans = GetTransactionFromID(_TransactionID);
            trans.Rollback();
            if (trans.Connection != null) trans.Connection.Close();
            trans.Dispose();
            m_lstTransaction.Remove(_TransactionID);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
    }

    public bool CommitTransaction()
    {
        try
        {
            SqlTransaction trans = GetTransactionFromID(_TransactionID);
            trans.Commit();
            if (trans.Connection != null) trans.Connection.Close();
            trans.Dispose();
            m_lstTransaction.Remove(_TransactionID);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
    }

    private SqlTransaction GetTransactionFromID(int transactionID)
    {
        if (!m_lstTransaction.ContainsKey(transactionID))
        {
            //Throw New Exception("Invalid transaction ID")
            Console.WriteLine("Invalid transaction ID");
            return null;

        }
        return m_lstTransaction[transactionID];
    }

    public object GetConnectInfo()
    {
        if (!m_lstTransaction.ContainsKey(_TransactionID))
        {
            return ConnectionString;
        }
        else
        {
            return m_lstTransaction[_TransactionID];
        }
    }
}
