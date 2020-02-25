using System;
using System.Collections.Generic;
using InvestmentBuilderCore;
using System.Data.SqlClient;
using System.Data;

namespace SQLServerDataLayer
{
    /// <summary>
    /// SQL Server implementation of the IInvestmentRecordInterface interface.
    /// </summary>
    public class SQLServerInvestmentRecordData : SQLServerBase, IInvestmentRecordInterface
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public SQLServerInvestmentRecordData(string connectionStr)
        {
            ConnectionStr = connectionStr;
        }

        /// <summary>
        /// Roll the specified investment from the previous valuation date to the new valuation date.
        /// </summary>
        public void RollInvestment(UserAccountToken userToken, string investment, DateTime dtValuation, DateTime dtPreviousValaution)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_RollInvestment", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtValuation));
                    command.Parameters.Add(new SqlParameter("@previousDate", dtPreviousValaution));
                    command.Parameters.Add(new SqlParameter("@company", investment));
                    command.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// UPdate the quantity of the specified investment. Will change the average price calculation for this
        /// investment but not the value.
        /// </summary>
        public void UpdateInvestmentQuantity(UserAccountToken userToken, string investment, DateTime dtValuation, double quantity)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_UpdateHolding", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@quantity", quantity));
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtValuation));
                    command.Parameters.Add(new SqlParameter("@company", investment));
                    command.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Add to quantity of specified investment in account specified in user token.
        /// </summary>
        public void AddNewShares(UserAccountToken userToken, string investment, double quantity, DateTime dtValaution, double dTotalCost)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_AddNewShares", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtValaution));
                    command.Parameters.Add(new SqlParameter("@company", investment));
                    command.Parameters.Add(new SqlParameter("@shares", quantity));
                    command.Parameters.Add(new SqlParameter("@totalCost", dTotalCost));
                    command.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// reduce quantity of specified investment for account specified in user token.
        /// </summary>
        public void SellShares(UserAccountToken userToken, string investment, double quantity, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_SellShares", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtValuation));
                    command.Parameters.Add(new SqlParameter("@company", investment));
                    command.Parameters.Add(new SqlParameter("@shares", quantity));
                    command.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Update the closing price for the specified investment for the specified accunt on the specified
        /// valuation date.
        /// </summary>
        public void UpdateClosingPrice(UserAccountToken userToken, string investment, DateTime dtValuation, double price)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var updateCommand = new SqlCommand("sp_UpdateClosingPrice", connection))
                {
                    updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    updateCommand.Parameters.Add(new SqlParameter("@valuationDate", dtValuation));
                    updateCommand.Parameters.Add(new SqlParameter("@investment", investment));
                    updateCommand.Parameters.Add(new SqlParameter("@closingPrice", price));
                    updateCommand.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Update the amount of dividends paid for the specified investment on the specified valuation date.
        /// </summary>
        public void UpdateDividend(UserAccountToken userToken, string investment, DateTime dtValuation, double dividend)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var updateCommand = new SqlCommand("sp_UpdateDividend", connection))
                {
                    updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    updateCommand.Parameters.Add(new SqlParameter("@valuationDate", dtValuation));
                    updateCommand.Parameters.Add(new SqlParameter("@company", investment));
                    updateCommand.Parameters.Add(new SqlParameter("@dividend", dividend));
                    updateCommand.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Returns the investment details for the specified investment.
        /// </summary>
        public InvestmentInformation GetInvestmentDetails(string investment)
        {
            InvestmentInformation data = null;
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetCompanyData", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Name", investment));
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var symbol = GetDBValue<string>("Symbol", reader, string.Empty);
                            var ccy = GetDBValue<string>("Currency", reader, string.Empty);
                            var exchange = GetDBValue<string>("Exchange", reader, string.Empty);
                            data = new InvestmentInformation(
                                symbol.Trim(),
                                exchange.Trim(),
                                ccy.Trim(),
                                GetDBValue<double>("ScalingFactor", reader)
                            );
                        }
                        reader.Close();
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Returns the list of active investments for the account specified in user token for the 
        /// specified date.
        /// </summary>
        public IEnumerable<KeyValuePair<string, double>> GetInvestments(UserAccountToken userToken, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetUserCompanies", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ValuationDate", dtValuation));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        yield return new KeyValuePair<string, double>(
                                                                GetDBValue<string>("Name", reader),
                                                                GetDBValue<double>("Price", reader));
                    }
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Create a new investment for the specified account on the specified valuation date.
        /// </summary>
        public void CreateNewInvestment(UserAccountToken userToken, string investment, string symbol, string currency, double quantity, double scalingFactor, double totalCost, double price, string exchange, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_CreateNewInvestment", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtValuation));
                    command.Parameters.Add(new SqlParameter("@investment", investment));
                    command.Parameters.Add(new SqlParameter("@symbol", symbol));
                    command.Parameters.Add(new SqlParameter("@currency", currency));
                    command.Parameters.Add(new SqlParameter("@scalingFactor", scalingFactor));
                    command.Parameters.Add(new SqlParameter("@shares", quantity));
                    command.Parameters.Add(new SqlParameter("@totalCost", totalCost));
                    command.Parameters.Add(new SqlParameter("@closingPrice", price));
                    command.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    command.Parameters.Add(new SqlParameter("@exchange", exchange ?? string.Empty));

                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Returns the investment details for the specified investmenton the spcified investment date.
        /// This includes all the calulcated values such as qunatity, price and net selling value.
        /// </summary>
        public IEnumerable<CompanyData> GetInvestmentRecordData(UserAccountToken userToken, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetLatestInvestmentRecords", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ValuationDate", dtValuation));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        double dTotalCost = GetDBValue<double>("TotalCost", reader);
                        double dSharesHeld = GetDBValue<double>("Bought", reader) + GetDBValue<double>("Bonus", reader) - GetDBValue<double>("Sold", reader);
                        double dAveragePrice = dTotalCost / dSharesHeld;
                        double dSharePrice = GetDBValue<double>("Price", reader);
                        double dDividend = GetDBValue<double>("Dividends", reader);

                        yield return new CompanyData
                        {
                            Name = GetDBValue<string>("Name", reader),
                            ValuationDate = dtValuation,
                            LastBrought = GetDBValue<DateTime>("LastBoughtDate", reader),
                            Quantity = dSharesHeld,
                            AveragePricePaid = dAveragePrice,
                            TotalCost = dTotalCost,
                            SharePrice = dSharePrice,
                            //dNetSellingValue = _GetNetSellingValue(dSharesHeld, dSharePrice),
                            Dividend = dDividend
                        };
                    }
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Deactivate the specified investment for the account specified in user token.
        /// </summary>
        public void DeactivateInvestment(UserAccountToken userToken, string investment)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_DeactivateCompany", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Name", investment));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Return latest Record investment value date for specified account. This differs from the latest account
        /// valuation date which is only generated when the account is valued by being added whenever an investment
        /// has changed.
        /// </summary>
        public DateTime? GetLatestRecordInvestmentValuationDate(UserAccountToken userToken)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetLatestRecordValuationDate", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var objResult = command.ExecuteScalar();
                    return objResult is DateTime ? (DateTime?)objResult : null;
                }
            }
        }

        /// <summary>
        /// returns the previously changed valuation date in the investment record from the specified valuation date. 
        /// so if  the investments were changed on the following dates:
        /// 03/03/2018
        /// 04/03/2018
        /// 05/03/2018
        /// and dtValuation is set to 04/03/208. This method will return 03/03/2018. if there is not a previous
        /// date value then it will return null.
        /// </summary>
        public DateTime? GetPreviousRecordInvestmentValuationDate(UserAccountToken userToken, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetPreviousRecordValuationDate", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.Parameters.Add(new SqlParameter("@ValuationDate", dtValuation));
                    var objResult = command.ExecuteScalar();
                    return objResult is DateTime ? (DateTime?)objResult : null;
                }
            }
        }

        /// <summary>
        /// Add the specified investment transactions to the investment records table for the specified 
        /// valuation date.
        /// </summary>
        public void AddTradeTransactions(IEnumerable<Stock> trades, TradeType action, UserAccountToken userToken, DateTime dtValuation)
        {
            if (trades == null)
                return;

            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                foreach (var trade in trades)
                {
                    var dtTransaction = trade.TransactionDate ?? DateTime.Today;
                    using (var command = new SqlCommand("sp_AddTransactionHistory", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@valuationDate", dtValuation));
                        command.Parameters.Add(new SqlParameter("@transactionDate", dtTransaction));
                        command.Parameters.Add(new SqlParameter("@company", trade.Name));
                        command.Parameters.Add(new SqlParameter("@action", action.ToString()));
                        command.Parameters.Add(new SqlParameter("@quantity", trade.Quantity));
                        command.Parameters.Add(new SqlParameter("@total_cost", trade.TotalCost));
                        command.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                        command.Parameters.Add(new SqlParameter("@user", userToken.User));
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        //Return the list of investment transactions between the following dates
        public Trades GetHistoricalTransactions(DateTime dtFrom, DateTime dtTo, UserAccountToken userToken)
        {
            List<Stock> buys = new List<Stock>();
            List<Stock> sells = new List<Stock>();
            List<Stock> changed = new List<Stock>();

            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetTransactionHistory", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@dateFrom", dtFrom));
                    command.Parameters.Add(new SqlParameter("@dateTo", dtTo));
                    command.Parameters.Add(new SqlParameter("@account", userToken.Account.AccountId));
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var action = (TradeType)Enum.Parse(typeof(TradeType), (string)reader["trade_action"]);
                        var trade = new Stock
                        {
                            Name = GetDBValue<string>("Name", reader),
                            Quantity = GetDBValue<double>("quantity", reader),
                            TotalCost = GetDBValue<double>("total_cost", reader)
                        };
                        switch (action)
                        {
                            case TradeType.BUY:
                                buys.Add(trade);
                                break;
                            case TradeType.SELL:
                                sells.Add(trade);
                                break;
                            case TradeType.MODIFY:
                                changed.Add(trade);
                                break;
                        }
                    }
                    reader.Close();
                }
            }
            return new Trades
            {
                Buys = buys.ToArray(),
                Sells = sells.ToArray(),
                Changed = changed.ToArray()
            };

        }

        /// <summary>
        /// Return the full investment details for the specified investment. This includes the 
        /// static investment details such as symbol etc.. and also the price details such as
        /// quantity, price, selling values etc..
        public IEnumerable<CompanyData> GetFullInvestmentRecordData(UserAccountToken userToken)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetFullInvestmentRecordData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        double dTotalCost = GetDBValue<double>("TotalCost", reader);
                        double dSharesHeld = GetDBValue<double>("Bought", reader) + GetDBValue<double>("Bonus", reader) - GetDBValue<double>("Sold", reader);
                        double dAveragePrice = dTotalCost / dSharesHeld;
                        double dSharePrice = GetDBValue<double>("Price", reader);
                        double dDividend = GetDBValue<double>("Dividends", reader);

                        yield return new CompanyData
                        {
                            Name = GetDBValue<string>("Name", reader),
                            ValuationDate = GetDBValue<DateTime>("ValuationDate", reader),
                            LastBrought = GetDBValue<DateTime>("LastBoughtDate", reader),
                            Quantity = dSharesHeld,
                            AveragePricePaid = dAveragePrice,
                            TotalCost = dTotalCost,
                            SharePrice = dSharePrice,
                            //dNetSellingValue = _GetNetSellingValue(dSharesHeld, dSharePrice),
                            Dividend = dDividend
                        };
                    }
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Returns true if the specified valuation date is a valid record valuation date for the account
        /// specified in user token.
        /// </summary>
        public bool IsExistingRecordValuationDate(UserAccountToken userToken, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_IsExistingRecordValuationDate", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@ValuationDate", dtValuation));
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var result = sqlCommand.ExecuteScalar();
                    return result != null;
                }
            }
        }
    }
}
