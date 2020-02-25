using System;
using System.Collections.Generic;

namespace InvestmentBuilderCore
{
    /// <summary>
    /// Interface to application data
    /// </summary>
    public interface IDataLayer
    {
        ///
        /// Client data interface
        /// </summary>
        IClientDataInterface ClientData { get; }

        /// <summary>
        /// InvestmentRecord data interface.
        /// </summary>
        IInvestmentRecordInterface InvestmentRecordData { get; }

        /// <summary>
        /// CashAccount data interface
        /// </summary>
        ICashAccountInterface CashAccountData { get; }

        /// <summary>
        /// Useraccount data interface.
        /// </summary>
        IUserAccountInterface UserAccountData { get; }

        /// <summary>
        /// Historical data interface.
        /// </summary>
        IHistoricalDataReader HistoricalData { get; }

        /// <summary>
        /// Method connects to a datasource.
        /// </summary>
        /// <param name="datasource"></param>
        void ConnectNewDatasource(string datasource);
    }

    /// <summary>
    /// client interface to datalayer contains client specific methods
    /// </summary>
    public interface IClientDataInterface
    {
        //Return a list of recent valuation dates for this account
        IEnumerable<DateTime> GetRecentValuationDates(UserAccountToken userToken, DateTime dtDateFrom);

        //Return a list of transaction types for the specified transaction (Payment or Reciept).
        IEnumerable<string> GetTransactionTypes(string side);        

        //Returns the latest valuation date for this account (if there is one)
        DateTime? GetLatestValuationDate(UserAccountToken userToken);

        DateTime? GetPreviousAccountValuationDate(UserAccountToken userToken, DateTime dtValuation);
        bool IsExistingValuationDate(UserAccountToken userToken, DateTime valuationDate);
        IEnumerable<string> GetAccountTypes();
        IEnumerable<string> GetAllCompanies();
        Stock GetTradeItem(UserAccountToken userToken, string name);
        int UndoLastTransaction(UserAccountToken userToken, DateTime fromValuationDate);
        Transaction GetLastTransaction(UserAccountToken userToken, DateTime fromValuationDate);
    }

    /// <summary>
    /// Defines the set of operations on the Investment Record table
    /// </summary>
    public interface IInvestmentRecordInterface
    {
        //investment record interface
        //roll (copy) an investment from dtPrevious to dtValuation
        void RollInvestment(UserAccountToken userToken, string investment, DateTime dtValuation, DateTime dtPreviousValaution);
        void UpdateInvestmentQuantity(UserAccountToken userToken, string investment, DateTime dtValuation, double quantity);
        void AddNewShares(UserAccountToken userToken, string investment, double quantity, DateTime dtValaution, double dTotalCost);
        void SellShares(UserAccountToken userToken, string investment, double quantity, DateTime dtValuation);
        void UpdateClosingPrice(UserAccountToken userToken, string investment, DateTime dtValuation, double price);
        void UpdateDividend(UserAccountToken userToken, string investment, DateTime dtValuation, double dividend);
        InvestmentInformation GetInvestmentDetails(string investment);
        IEnumerable<KeyValuePair<string, double>> GetInvestments(UserAccountToken userToken, DateTime dtValuation);
        void CreateNewInvestment(UserAccountToken userToken, string investment, string symbol, string currency,
                                 double quantity, double scalingFactor, double totalCost, double price,
                                 string exchange, DateTime dtValuation);
        IEnumerable<CompanyData> GetInvestmentRecordData(UserAccountToken userToken, DateTime dtValuation);
        void DeactivateInvestment(UserAccountToken userToken, string investment);
        DateTime? GetLatestRecordInvestmentValuationDate(UserAccountToken userToken);
        DateTime? GetPreviousRecordInvestmentValuationDate(UserAccountToken userToken, DateTime dtValuation);
        void AddTradeTransactions(IEnumerable<Stock> trades, TradeType action, UserAccountToken userToken, DateTime dtValuation);
        Trades GetHistoricalTransactions(DateTime dtFrom, DateTime dtTo, UserAccountToken userToken);
        //IEnumerable<CompanyData> GetCompanyRecordData(UserAccountToken userToken, string company);
        //returns the full investment record data set for this account
        IEnumerable<CompanyData> GetFullInvestmentRecordData(UserAccountToken userToken);
        bool IsExistingRecordValuationDate(UserAccountToken userToken, DateTime dtValuation);
    }

    public interface ICashAccountInterface
    {
        CashAccountData GetCashAccountData(UserAccountToken userToken, DateTime valuationDate);
        int AddCashAccountTransaction(UserAccountToken userToken, DateTime valuationDate, DateTime transactionDate,
                                string type, string parameter, double amount);

        void RemoveCashAccountTransaction(UserAccountToken userToken, int transactionID);
        void GetCashAccountTransactions(UserAccountToken userToken, string side, DateTime valuationDate, Action<System.Data.IDataReader> fnAddTransaction);
        double GetBalanceInHand(UserAccountToken userToken, DateTime valuationDate);
    }

    public interface IUserAccountInterface
    {
       //user account interface
        void RollbackValuationDate(UserAccountToken userToken, DateTime dtValuation);
        void UpdateMemberAccount(UserAccountToken userToken, DateTime dtValuation, string member, double dAmount);
        double GetMemberSubscription(UserAccountToken userToken, DateTime dtValuation, string member);
        IEnumerable<KeyValuePair<string, double>> GetMemberAccountData(UserAccountToken userToken, DateTime dtValuation);
        double GetPreviousUnitValuation(UserAccountToken userToken, DateTime? previousDate);
        void SaveNewUnitValue(UserAccountToken userToken, DateTime dtValuation, double dUnitValue);
        double GetIssuedUnits(UserAccountToken userToken, DateTime dtValuation);
        AccountModel GetUserAccountData(UserAccountToken userToken);
        double GetStartOfYearValuation(UserAccountToken userToken, DateTime valuationDate);
        IEnumerable<Redemption> GetRedemptions(UserAccountToken userToken, DateTime valuationDate);
        void AddRedemption(UserAccountToken userToken, string user, DateTime transactionDate, double amount);
        RedemptionStatus UpdateRedemption(UserAccountToken userToken, string user, DateTime transactionDate, double amount, double units);
        void UpdateMemberForAccount(UserAccountToken userToken, string member, AuthorizationLevel level, bool add);
        int CreateAccount(UserAccountToken userToken, AccountModel account);
        void UpdateAccount(UserAccountToken userToken, AccountModel account);
        AccountModel GetAccount(UserAccountToken userToken);
        IEnumerable<string> GetAccountMembers(UserAccountToken userToken, DateTime valuationDate);

        /// <summary>
        /// returns a list of accounts that this user is a member of.
        /// </summary>
        IEnumerable<AccountMember> GetAccountMemberDetails(UserAccountToken userToken, DateTime valuationDate);
        IEnumerable<AccountIdentifier> GetAccountNames(string user, bool bCheckAdmin);
        IEnumerable<string> GetActiveCompanies(UserAccountToken userToken, DateTime valuationDate);
        bool InvestmentAccountExists(AccountIdentifier accountID);
        IEnumerable<double> GetUnitValuationRange(UserAccountToken userToken, DateTime dateFrom, DateTime dateTo);
        int GetUserId(string userName);
        void AddUser(string userName, string description);
    }

    public interface IHistoricalDataReader
    {
        IEnumerable<HistoricalData> GetHistoricalAccountData(UserAccountToken userToken);
        string GetIndexHistoricalData(UserAccountToken userToken, string symbol);
    }
}
