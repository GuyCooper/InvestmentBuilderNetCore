using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using System.Data.SqlClient;
using System.Data;
using NLog;

namespace SQLServerDataLayer
{
    /// <summary>
    /// SQL Server implementation of IUserAccountInterface interface.
    /// </summary>
    public class SQLServerUserAccountData : SQLServerBase, IUserAccountInterface
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public SQLServerUserAccountData(string connectionStr)
        {
            ConnectionStr = connectionStr;
        }

        /// <summary>
        /// Rollback the valuation for the specified valuation date for the account specified
        /// in user token.
        /// </summary>
        public void RollbackValuationDate(UserAccountToken userToken, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var updateCommand = new SqlCommand("sp_RollbackUpdate", connection))
                {
                    updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    updateCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    updateCommand.Parameters.Add(new SqlParameter("@ValuationDate", dtValuation));
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// update the specified users capital account.
        public void UpdateMemberAccount(UserAccountToken userToken, DateTime dtValuation, string member, double dAmount)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var updateCommand = new SqlCommand("sp_UpdateMembersCapitalAccount", connection))
                {
                    updateCommand.CommandType = System.Data.CommandType.StoredProcedure;
                    updateCommand.Parameters.Add(new SqlParameter("@ValuationDate", dtValuation.Date));
                    updateCommand.Parameters.Add(new SqlParameter("@Member", member));
                    updateCommand.Parameters.Add(new SqlParameter("@Units", dAmount));
                    updateCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Return the total subscription for the specified user on the account specified in user token
        /// on the specified valuation date.
        /// </summary>
        public double GetMemberSubscription(UserAccountToken userToken, DateTime dtValuation, string member)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            double dSubscription = 0d;
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetMemberSubscriptionAmount", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Member", member));
                    command.Parameters.Add(new SqlParameter("@ValuationDate", dtValuation.Date));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            dSubscription = reader.GetDouble(0);
                        }
                        reader.Close();
                    }
                }
            }
            return dSubscription;
        }

        /// <summary>
        /// Returns the total capital for the specified member on the account specified in user token for the
        /// specified valuation date.
        /// </summary>
        public IEnumerable<KeyValuePair<string, double>> GetMemberAccountData(UserAccountToken userToken, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetMembersCapitalAccount", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@ValuationDate", dtValuation.Date));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var member = GetDBValue<string>("Member", reader);
                            var units = GetDBValue<double>("Units", reader);
                            yield return new KeyValuePair<string, double>(member, units);
                        }
                        reader.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the previous unit price for the account specified in user token from the optional
        /// previous date. 
        /// </summary>
        public double GetPreviousUnitValuation(UserAccountToken userToken, DateTime? previousDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            if (previousDate.HasValue == false)
            {
                return 1d;  //if first time then unit value starts at 1
            }

            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetUnitValuation", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", previousDate.Value));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    return (double)command.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Add a new unit valuation for the account on the specified valuation date
        /// </summary>
        public void SaveNewUnitValue(UserAccountToken userToken, DateTime dtValuation, double dUnitValue)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_AddNewUnitValuation", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtValuation));
                    command.Parameters.Add(new SqlParameter("@unitValue", dUnitValue));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// return the total number of issued units for an account on the specified valuation date.
        /// </summary>
        public double GetIssuedUnits(UserAccountToken userToken, DateTime dtValuation)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetIssuedUnits", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtValuation.Date));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));

                    var result = command.ExecuteScalar();
                    if (result is double)
                    {
                        return (double)result;
                    }
                }
            }
            return 0d;
        }

        /// <summary>
        /// returns the account details for the account defined in the usertoken.
        /// </summary>
        public AccountModel GetUserAccountData(UserAccountToken userToken)
        {
            if(userToken.HasInvalidAccount() == true)
            {
                return null;
            }

            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetAccountData", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new AccountModel
                            (
                                userToken.Account,
                                GetDBValue<string>("Description", reader),
                                (string)reader["Currency"],
                                null,
                                true,
                                GetDBValue<string>("Broker", reader),
                                null
                            );
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the unit valuation for an account at the start of year relative to the specifed
        /// valuation date.
        /// </summary>
        public double GetStartOfYearValuation(UserAccountToken userToken, DateTime valuationDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);

            DateTime dtStartOfYear = valuationDate.Month > 1 ? valuationDate.AddMonths(1 - valuationDate.Month) : valuationDate;
            if (dtStartOfYear.Day > 1)
                dtStartOfYear = dtStartOfYear.AddDays(1 - dtStartOfYear.Day);

            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetStartOfYearValuation", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@valuationDate", dtStartOfYear));
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    var objResult = command.ExecuteScalar();
                    if (objResult != null)
                    {
                        return (double)command.ExecuteScalar();
                    }
                }
            }
            return 1d;
        }

        /// <summary>
        /// Returns a list of any redemptions for an account for the specified valuation date. These are
        /// all redemptions issued between the valuation date and the previous valuation date.
        /// </summary>
        public IEnumerable<Redemption> GetRedemptions(UserAccountToken userToken, DateTime valuationDate)
        {
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_GetRedemptions", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    sqlCommand.Parameters.Add(new SqlParameter("@TransactionDate", valuationDate));
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            yield return new Redemption
                            (
                                GetDBValue<string>("UserName", reader),
                                GetDBValue<double>("amount", reader),
                                GetDBValue<DateTime>("transaction_date", reader),
                                (RedemptionStatus)Enum.Parse(typeof(RedemptionStatus), (string)reader["status"])
                            );
                        }
                    }
                }
            }
        }
 
        /// <summary>
        /// Add a redemption request to an account with the specified transaction date.
        /// </summary>
        public void AddRedemption(UserAccountToken userToken, string user, DateTime transactionDate, double amount)
        {
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);

            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_AddRedemption", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.Parameters.Add(new SqlParameter("@User", user));
                    command.Parameters.Add(new SqlParameter("@TransactionDate", transactionDate.Date));
                    command.Parameters.Add(new SqlParameter("@Amount", amount));
                    command.Parameters.Add(new SqlParameter("@Status", RedemptionStatus.Pending.ToString()));

                    command.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Update a requested redemtion on an account. This is used for changing the status of the redemtion to complete
        /// and specifiying the actual amount redeemed.
        /// </summary>
        public RedemptionStatus UpdateRedemption(UserAccountToken userToken, string user, DateTime transactionDate, double amount, double units)
        {
            var result = RedemptionStatus.Complete;
            userToken.AuthorizeUser(AuthorizationLevel.UPDATE);

            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_UpdateRedemption", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.Parameters.Add(new SqlParameter("@User", user));
                    command.Parameters.Add(new SqlParameter("@TransactionDate", transactionDate.Date));
                    command.Parameters.Add(new SqlParameter("@Amount", amount));
                    command.Parameters.Add(new SqlParameter("@UnitsRedeemed", units));
                    command.Parameters.Add(new SqlParameter("@Status", result.ToString()));

                    command.ExecuteScalar();
                }
            }

            return result;
        }

        /// <summary>
        /// return a list of all the account members for an account on a specified valuation date.
        /// </summary>
        public IEnumerable<string> GetAccountMembers(UserAccountToken userToken, DateTime valuationDate)
        {
            return GetAccountMemberDetails(userToken, valuationDate).Select(x => x.Name);
        }

        /// <summary>
        /// return a list of all the account members for an account on a specified valuation date.
        /// </summary>
        public IEnumerable<AccountMember> GetAccountMemberDetails(UserAccountToken userToken, DateTime valuationDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetAccountMembers", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.Parameters.Add(new SqlParameter("@ValuationDate", valuationDate));
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new AccountMember(
                                GetDBValue<string>("UserName", reader),
                                (AuthorizationLevel)reader["Authorization"]
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add or update a member for an account
        /// </summary>
        public void UpdateMemberForAccount(UserAccountToken userToken, string member, AuthorizationLevel level, bool add)
        {
            userToken.AuthorizeUser(AuthorizationLevel.ADMINISTRATOR);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_UpdateMemberForAccount", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    sqlCommand.Parameters.Add(new SqlParameter("@Member", member));
                    sqlCommand.Parameters.Add(new SqlParameter("@Level", (int)level));
                    sqlCommand.Parameters.Add(new SqlParameter("@Add", add ? 1 : 0));
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Update an existing account
        /// </summary>
        public void UpdateAccount(UserAccountToken userToken, AccountModel account)
        {
            userToken.AuthorizeUser(AuthorizationLevel.ADMINISTRATOR);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_UpdateAccount", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", account.Identifier.AccountId));
                    sqlCommand.Parameters.Add(new SqlParameter("@Currency", account.ReportingCurrency));
                    sqlCommand.Parameters.Add(new SqlParameter("@AccountType", account.Type));
                    sqlCommand.Parameters.Add(new SqlParameter("@Enabled", account.Enabled));
                    sqlCommand.Parameters.Add(new SqlParameter("@Description", account.Description));
                    sqlCommand.Parameters.Add(new SqlParameter("@Broker", account.Broker));
                    sqlCommand.ExecuteNonQuery();
                }
            }

        }

        /// <summary>
        /// Create a new account.
        /// </summary>
        public int CreateAccount(UserAccountToken userToken, AccountModel account)
        {
            Int32 result = 0;
            userToken.AuthorizeUser(AuthorizationLevel.ADMINISTRATOR);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_CreateAccount", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@Name", account.Identifier.Name));
                    sqlCommand.Parameters.Add(new SqlParameter("@Currency", account.ReportingCurrency));
                    sqlCommand.Parameters.Add(new SqlParameter("@AccountType", account.Type));
                    sqlCommand.Parameters.Add(new SqlParameter("@Enabled", account.Enabled));
                    sqlCommand.Parameters.Add(new SqlParameter("@Description", account.Description));
                    sqlCommand.Parameters.Add(new SqlParameter("@Broker", account.Broker));
                    var objResult = sqlCommand.ExecuteScalar();
                    if (objResult != null)
                    {
                        result = (int)Math.Floor((Decimal)objResult);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Return the account details for the specified account.
        /// </summary>
        public AccountModel GetAccount(UserAccountToken userToken)
        {
            userToken.AuthorizeUser(AuthorizationLevel.ADMINISTRATOR);
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_GetAccountData", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            //var obj = reader["Enabled"];
                            return new AccountModel(userToken.Account,
                                                    GetDBValue<string>("Description", reader),
                                                    GetDBValue<string>("Currency", reader),
                                                    GetDBValue<string>("AccountType", reader),
                                                    (byte)reader["Enabled"] != 0 ? true : false,
                                                    GetDBValue<string>("Broker", reader),
                                                    null
                                                    );
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// returns a list of accounts that this user is a member of.
        /// </summary>
        public IEnumerable<AccountIdentifier> GetAccountNames(string user, bool bCheckAdmin)
        {
            logger.Info($"GetAccountNames for user {user}");
            //return the list of accounts that this user is able to see
            using (var connection = OpenConnection())
            {
                using (var sqlCommand = new SqlCommand("sp_GetAccountsForUser", connection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@User", user));
                    sqlCommand.Parameters.Add(new SqlParameter("@CheckAdmin", bCheckAdmin ? 1 : 0));
                    using (var reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return new AccountIdentifier
                            {
                                Name = GetDBValue<string>("Name", reader),
                                AccountId = GetDBValue<int>("Account_Id", reader)
                            };
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Return the list of active investments for this account.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetActiveCompanies(UserAccountToken userToken, DateTime valuationDate)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetActiveCompanies", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.Parameters.Add(new SqlParameter("@ValuationDate", valuationDate));
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return (string)reader["Name"];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Return true if this account already exists otherwise return false
        /// </summary>
        /// <returns></returns>
        public bool InvestmentAccountExists(AccountIdentifier accountName)
        {
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_InvestmentAccountExists", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", accountName.AccountId));
                    var objResult = command.ExecuteScalar();
                    if (objResult != null)
                    {
                        return (int)objResult == 1;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a list of unit valuations for an account with the specified dateFrom - dateTo range.
        /// </summary>
        public IEnumerable<double> GetUnitValuationRange(UserAccountToken userToken, DateTime dateFrom, DateTime dateTo)
        {
            userToken.AuthorizeUser(AuthorizationLevel.READ);
            var ret = new List<double>();
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetUnitValuationRange", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Account", userToken.Account.AccountId));
                    command.Parameters.Add(new SqlParameter("@dateFrom", dateFrom));
                    command.Parameters.Add(new SqlParameter("@dateTo", dateTo));
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ret.Add((double)reader["Unit_Price"]);
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// return the user id for the specified user. throws an exception if the user does not exist.
        /// </summary>
        public int GetUserId(string userName)
        {
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_GetUserId", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@UserName", userName));
                    var objResult = command.ExecuteScalar();
                    if (objResult != null)
                    {
                        return (int)objResult;
                    }
                 }
            }
            return -1;
        }

        /// <summary>
        /// Add a new user to the database
        /// </summary>
        public void AddUser(string userName, string description)
        {
            using (var connection = OpenConnection())
            {
                using (var command = new SqlCommand("sp_AddNewUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@UserName", userName));
                    command.Parameters.Add(new SqlParameter("@Description", description));
                    command.ExecuteNonQuery();
                }
            }
        }

        private static Logger logger = LogManager.GetCurrentClassLogger();
    }
}
