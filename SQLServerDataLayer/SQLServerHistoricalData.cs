using System;
using System.Collections.Generic;
using InvestmentBuilderCore;
using System.Data.SqlClient;
namespace SQLServerDataLayer
{
    /// <summary>
    /// SQLServer implementation of IHistoricalData interface.
    /// </summary>
    class SQLServerHistoricalData : SQLServerBase, IHistoricalDataReader
    {
        public SQLServerHistoricalData(string connectionStr)
        {
            ConnectionStr = connectionStr;
        }

        /// <summary>
        /// Method returns all the historial unit prices for this account. 
        /// </summary>
        public IEnumerable<HistoricalData> GetHistoricalAccountData(UserAccountToken userToken)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetUnitPriceData", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new HistoricalData
                            (
                                date: GetDBValue<DateTime>("Valuation_Date", reader),
                                price: GetDBValue<double>("Unit_Price", reader)
                            );
                        }
                        reader.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Returns all the historical prices for the index specified by the symbol
        /// </summary>
        public string GetIndexHistoricalData(UserAccountToken userToken, string symbol)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            string result = null;
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetHistoricalData", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Symbol", symbol));
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result = GetDBValue<string>("Data", reader);
                        }
                        reader.Close();
                    }
                }
            }
            return result;
        }
    }
}
