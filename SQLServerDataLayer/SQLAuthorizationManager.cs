using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using System.Data.SqlClient;

namespace SQLServerDataLayer
{
    /// <summary>
    /// implementation class returns the authorization level for a user   
    /// </summary>
    public class SQLAuthorizationManager : AuthorizationManager, IDisposable
    {
        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public SQLAuthorizationManager(IConfigurationSettings settings)
        {
            _connectionStr = settings.DatasourceString;
        }

        /// <summary>
        /// Set the datasource for this class.
        /// </summary>
        public void ConnectNewDatasource(string datasource)
        {
            _connectionStr = datasource;
        }

        public void Dispose()
        {
            //_connection.Close();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Returns true if specified user is a global administrator.
        /// </summary>
        protected override bool IsGlobalAdministrator(string user)
        {
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_IsAdministrator", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@User", user));
                    var objResult = command.ExecuteScalar();
                    if (objResult != null)
                    {
                        return string.Equals((string)objResult, user, StringComparison.InvariantCultureIgnoreCase);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns the authorisation level for the speciifed user against the specified account.
        /// </summary>
        protected override AuthorizationLevel GetUserAuthorizationLevel(string user, AccountIdentifier account)
        {
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetAuthorizationLevel", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@User", user));
                    command.Parameters.Add(new SqlParameter("@Account", account.AccountId));
                    var objResult = command.ExecuteScalar();
                    if (objResult != null)
                    {
                        return (AuthorizationLevel)objResult;
                    }
                }
            }
            return AuthorizationLevel.NONE;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Open the database connection
        /// </summary>
        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(_connectionStr);
            connection.Open();
            return connection;
        }

        #endregion

        #region Private Data

        private string _connectionStr;

        #endregion
    }
}
