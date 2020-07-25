using System;
using System.Data.SQLite;
using Dapper;
using System.Data;
using System.Threading.Tasks;

namespace b_118.Database
{
    class DatabaseConnection
    {
        private readonly string _databaseName;
        private readonly int _version;
        private bool _hasInitialized = false;

        public DatabaseConnection(string databaseName)
        {
            _databaseName = databaseName;
            _version = 3;
            if (!System.IO.File.Exists($"{databaseName}.sqlite"))
            {
                SQLiteConnection.CreateFile($"{databaseName}.sqlite");
            }
        }

        public DatabaseConnection(string databaseName, int version)
        {
            _databaseName = databaseName;
            _version = version;
            if (!System.IO.File.Exists($"{databaseName}.sqlite"))
            {
                SQLiteConnection.CreateFile($"{databaseName}.sqlite");
            }
        }

        private string GetDatabaseFileName()
        {
            return $"{_databaseName}.sqlite";
        }

        private string GetDatabaseConnectionString()
        {
            return $"Data Source={GetDatabaseFileName()};Version={_version};";
        }

        public async Task<bool> Init(string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (_hasInitialized)
                return _hasInitialized;
            try
            {
                SQLiteConnection connection = new SQLiteConnection(GetDatabaseConnectionString());
                await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
                _hasInitialized = true;
            } catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
            return _hasInitialized;
        }

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(GetDatabaseConnectionString());
        }


    }
}
