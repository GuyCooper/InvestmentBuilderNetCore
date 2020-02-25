using System;
using System.Collections.Generic;
using InvestmentBuilderCore;
using System.Data.SqlClient;
using System.Data;

namespace SQLServerDataLayer
{
    /// <summary>
    /// SQL Serverimplementation of the IClientDataInterface
    /// </summary>
    public class SQLServerClientData : SQLServerBase, IClientDataInterface
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SQLServerClientData(string connectionStr)
        {
            ConnectionStr = connectionStr;
        }

        /// <summary>
        /// Retuns a list of the most recent valuation dates to the specified date for the account specified in the
        /// user token.
        /// </summary>
        public IEnumerable<DateTime> GetRecentValuationDates(UserAccountToken userToken, DateTime dtDateFrom)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);

            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_RecentValuationDates", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.Parameters.Add(new SqlParameter("@DateFrom", dtDateFrom));
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader.GetDateTime(0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of the possible transaction types for the side (receipt or payment)
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public IEnumerable<string> GetTransactionTypes(string side)
        {
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetTransactionTypes", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@side", side));
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return GetDBValue<string>("type", reader);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the latest valuation date for the account specified in the user token
        /// </summary>
        public DateTime? GetLatestValuationDate(UserAccountToken userToken)
        {
            if(userToken.HasInvalidAccount())
            {
                return null;
            }

            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_GetLatestValuationDate", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetDateTime(0);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if the specified valuation date is a valid valuation date for the account
        /// specified in user token.
        /// </summary>
        public bool IsExistingValuationDate(UserAccountToken userToken, DateTime valuationDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_IsExistingValuationDate", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@ValuationDate", valuationDate));
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var result = sqlCommand.ExecuteScalar();
                    return result != null;
                }
            }
        }

        /// <summary>
        /// Returns alist of the possible account types (personal or club)
        /// </summary>
        public IEnumerable<string> GetAccountTypes()
        {
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("SELECT [Type] FROM AccountTypes", connection))
                {
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader.GetString(0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of all instruments (companies stored in the database.
        /// </summary>
        public IEnumerable<string> GetAllCompanies()
        {
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("SELECT [Name] FROM Companies", connection))
                {
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader.GetString(0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retuns the instrument details for the spcified instrument (TradeItem or Company)
        /// </summary>
        public Stock GetTradeItem(UserAccountToken userToken, string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                return null;
            }

            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_GetTradeItem", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@Company", name));
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            //var obj = reader["Enabled"];
                            double quantity = GetDBValue<double>("Shares_Bought", reader) - GetDBValue<double>("Shares_Sold", reader);
                            return new Stock
                            {
                                Name = GetDBValue<string>("Name", reader),
                                TransactionDate = GetDBValue<DateTime>("LastBoughtDate", reader),
                                Symbol = GetDBValue<string>("Symbol", reader),
                                Exchange = GetDBValue<string>("Exchange", reader),
                                Currency = GetDBValue<string>("Currency", reader),
                                Quantity = quantity,
                                TotalCost = GetDBValue<double>("Total_Cost", reader),
                                ScalingFactor = GetDBValue<double>("ScalingFactor", reader)
                            };
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Undo the last transaction for the account specified in the user token.
        /// Returns the numbers of rows affected
        /// </summary>
        public int UndoLastTransaction(UserAccountToken userToken, DateTime fromValuationDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_UndoLastTransaction", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    sqlCommand.Parameters.Add(new SqlParameter("@fromValuationDate", fromValuationDate));

                    return sqlCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Returns the last investment transaction (BUY,SELL, CHANGE)
        /// </summary>
        /// <param name="userToken"></param>
        /// <returns></returns>
        public Transaction GetLastTransaction(UserAccountToken userToken, DateTime fromValuationDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_GetLastTransaction", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    sqlCommand.Parameters.Add(new SqlParameter("@fromValuationDate", fromValuationDate));
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Transaction
                            {
                                InvestmentName = GetDBValue<string>("Name", reader),
                                Quantity = GetDBValue<double>("quantity", reader),
                                Amount = GetDBValue<double>("total_cost", reader),
                                TransactionType = (TradeType)Enum.Parse(typeof(TradeType), GetDBValue<string>("trade_action", reader))
                            };
                        }
                    }
                }
            }
            return null;

        }

        /// <summary>
        /// Return the previous account valuation date for the spcified account to the date specified.
        /// </summary>
        public DateTime? GetPreviousAccountValuationDate(UserAccountToken userToken, DateTime dtValuation)
        {
            if(userToken.Account == null || userToken.Account.AccountId == 0)
            {
                //user is not a member of any account
                return null;
            }
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            DateTime? dtPrevious = null;
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetPreviousValuationDate", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtValuation.Date));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        dtPrevious = (DateTime)result;
                    }
                }
            }
            return dtPrevious;
        }
    }
}
