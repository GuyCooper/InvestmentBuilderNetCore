using System;
using InvestmentBuilderCore;
using System.Data.SqlClient;
using System.Data;

namespace SQLServerDataLayer
{
    /// <summary>
    /// SQL Server implementation of ICashAccountData interface
    /// </summary>
    public class SQLServerCashAccountData : SQLServerBase, ICashAccountInterface
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SQLServerCashAccountData(string connectionStr)
        {
            ConnectionStr = connectionStr;
        }

        /// <summary>
        /// Return the CashAccount data for the account specified in UserToken on the specified
        /// valuation date.
        /// </summary>
        public CashAccountData GetCashAccountData(UserAccountToken userToken, DateTime valuationDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);

            var cashData = new CashAccountData();

            //retrieve the current bank balance
            using (var connection = OpenConnection())
            {
                using (SqlCommand cmdBankBalance = new SqlCommand("sp_GetBankBalance", connection))
                {
                    cmdBankBalance.CommandType = System.Data.CommandType.StoredProcedure;
                    cmdBankBalance.Parameters.Add(new SqlParameter("@ValuationDate", valuationDate.Date));
                    cmdBankBalance.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));

                    //cashData.BankBalance = balanceParam.Value is double ? (double)balanceParam.Value : 0d;
                    var oBalance = cmdBankBalance.ExecuteScalar();
                    if (oBalance is double)
                    {
                        cashData.BankBalance = (double)cmdBankBalance.ExecuteScalar();
                    }

                    using (SqlCommand cmdDividends = new SqlCommand("sp_GetDividends", connection))
                    {
                        cmdDividends.CommandType = System.Data.CommandType.StoredProcedure;
                        cmdDividends.Parameters.Add(new SqlParameter("@ValuationDate", valuationDate.Date));
                        cmdDividends.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                        using (SqlDataReader reader = cmdDividends.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var company = GetDBValue<string>("Company", reader);
                                var dividend = GetDBValue<double>("Dividend", reader);
                                if (cashData.Dividends.ContainsKey(company) == true)
                                {
                                    cashData.Dividends[company] += dividend;
                                }
                                else
                                {
                                    cashData.Dividends.Add(company, dividend);
                                }

                            }
                        }
                    }
                }
            }
            return cashData;
        }

        /// <summary>
        /// Remove the specifed cash account transaction on the specifed date. Usually added in error.
        /// </summary>
        public void RemoveCashAccountTransaction(UserAccountToken userToken, int transactionID)
        {
            using (var connection = OpenConnection())
            {
                userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
                using (var sqlCommand = new SqlCommand("sp_RemoveCashAccountData", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@TransactionID", transactionID));
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Return all the cash account transactions for the specifed account with the specied vauation date for the specified side.
        /// </summary>
        public void GetCashAccountTransactions(UserAccountToken userToken, string side, DateTime valuationDate, Action<System.Data.IDataReader> fnAddTransaction)
        {
            if (userToken.Account == null || userToken.Account.AccountId == 0 || fnAddTransaction == null)
            {
                return;
            }

            userToken.AuthorizeUser(AuthorizationLevel.READ);

            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_GetCashAccountData", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@ValuationDate", valuationDate.Date));
                    sqlCommand.Parameters.Add(new SqlParameter("@Side", side));
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fnAddTransaction(reader);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Return the balanceinhand for the specifed account on the specified valuation date.
        /// </summary>
        public double GetBalanceInHand(UserAccountToken userToken, DateTime valuationDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            double result = 0d;
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_GetBalanceInHand", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@ValuationDate", valuationDate.Date));
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result += reader.GetDouble(0);
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Add a cash account transaction for the specified account on the specified valuation date
        /// return a unique id for the transaction (transaction_id)
        /// </summary>
        public int AddCashAccountTransaction(UserAccountToken userToken, DateTime valuationDate, DateTime transactionDate, string type, string parameter, double amount)
        {
            int result = 0;
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_AddCashAccountData", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@ValuationDate", valuationDate.Date));
                    sqlCommand.Parameters.Add(new SqlParameter("@TransactionDate", transactionDate));
                    sqlCommand.Parameters.Add(new SqlParameter("@TransactionType", type));
                    sqlCommand.Parameters.Add(new SqlParameter("@Parameter", parameter));
                    sqlCommand.Parameters.Add(new SqlParameter("@Amount", amount));
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var objResult = sqlCommand.ExecuteScalar();
                    if (objResult != null)
                    {
                        result = (int)Math.Floor((Decimal)objResult);
                    }

                }
            }
            return result;
        }
    }
}
