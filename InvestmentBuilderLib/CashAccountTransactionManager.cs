using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using NLog;
using System.Diagnostics.Contracts;

namespace InvestmentBuilderLib
{
    //transaction class.used for binding the cash account transactions to a displayable
    //view.
    public abstract class CashTransaction
    {
        public int TransactionID { get; set; }
        public DateTime ValuationDate { get; set; }
        //public DateTime TransactionDate { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string Parameter { get; set; }
        public double Amount { get; set; }
        public bool Added { get; set; }
        public bool IsTotal { get; set; }
    }

    /// <summary>
    /// Receipt transaction class.
    /// </summary>
    public class ReceiptTransaction : CashTransaction
    {
        public double Subscription { get; set; }
        public double Sale { get; set; }
        public double Dividend { get; set; }
        public double Other { get; set; }
    }

    /// <summary>
    /// Payment transaction class.
    /// </summary>
    public class PaymentTransaction : CashTransaction
    {
        public double Withdrawls { get; set; }
        public double Purchases { get; set; }
        public double Other { get; set; }
    }

    /// <summary>
    /// Class manages all cash transactions
    /// </summary>
    public sealed class CashAccountTransactionManager
    {
        #region Public Properties

        public string PaymentMnemomic { get { return "P"; } }
        public string ReceiptMnemomic { get { return "R"; } }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor
        /// </summary>
        public CashAccountTransactionManager(IDataLayer dataLayer)
        {
            _cashAccountData = dataLayer.CashAccountData;

            _transactionLookup = new Dictionary<string, Dictionary<string, string>>
            {
                {ReceiptMnemomic, _receiptTransactionLookup},
                {PaymentMnemomic, _paymentTransactionLookup}
            };
        }

        public IEnumerable<string> GetTransactionTypes(string mnenomic)
        {
            return _transactionLookup[mnenomic].Keys;
        }

        /// <summary>
        /// Return the list of payment transactions for the specified user between the previous
        /// valuation date and the valuation date.
        /// </summary>

        public IList<PaymentTransaction> GetPaymentTransactions(UserAccountToken userToken, DateTime dtValuationDate, out double dTotal)
        {
            Contract.Requires(userToken != null);            
            var transactions = new List<PaymentTransaction>();
            _GetTransactionsImpl<PaymentTransaction>(userToken, dtValuationDate, PaymentMnemomic,
                                                        transactions);
            dTotal = _AddTotalRow<PaymentTransaction>(dtValuationDate, transactions);
            return transactions;
        }

        /// <summary>
        /// Return the list of receipt transactions for the specified user between the previous
        /// valuation date and the valuation date.
        /// </summary>
        public IList<ReceiptTransaction> GetReceiptTransactions(UserAccountToken userToken, DateTime dtValuationDate, DateTime? dtPreviousValuationDate, 
                                            out double dTotal)
        {
            Contract.Requires(userToken != null);
            var transactions = new List<ReceiptTransaction>();
            _GetTransactionsImpl(userToken, dtValuationDate, ReceiptMnemomic,
                                                        transactions);
            //add the balance in handfrom the previous monthif it is not already there
            //can only do this if user is an adminstrator
            if(userToken.IsAdministrator && dtPreviousValuationDate.HasValue && dtValuationDate > dtPreviousValuationDate)
            {
                var balanceInHand = transactions.FirstOrDefault(r => r.TransactionType == BALANCEINHAND);
                if(balanceInHand == null)
                {
                    var dAmount = _cashAccountData.GetBalanceInHand(userToken, dtPreviousValuationDate.Value);
                    
                    var transaction = new ReceiptTransaction
                    {
                        Parameter = BALANCEINHAND,
                        Added = true,
                        TransactionDate = dtValuationDate.ToString("yyyy-MM-dd"),
                        TransactionType = BALANCEINHAND,
                        Subscription = dAmount,
                        Amount = dAmount
                    };

                    transactions.Add(transaction);                    
                    //we also need to add the balance in hand transaction to the database
                    //so the validation will work
                    AddTransaction(userToken, dtValuationDate, dtValuationDate, transaction.TransactionType,
                        transaction.Parameter, transaction.Subscription);
                }
                else
                {
                    //move the balance in hand transaction to the top of the list
                    transactions.Remove(balanceInHand);
                    transactions.Insert(0, balanceInHand);
                }
            }
            dTotal = _AddTotalRow<ReceiptTransaction>(dtValuationDate, transactions);
            return transactions;
        }

        /// <summary>
        /// Add a cash transaction.
        /// </summary>
        public int AddTransaction(UserAccountToken userToken, DateTime dtValuationDate, DateTime dtTransactionDate,
                                        string type, string parameter, double amount)
        {
            Contract.Requires(userToken != null);
            Contract.Requires(string.IsNullOrEmpty(type) == false);

            logger.Log(userToken, LogLevel.Info, "adding cash transaction. type: {0}, parameter: {1}, amount {2}", type, parameter, amount);
            return _cashAccountData.AddCashAccountTransaction(userToken, dtValuationDate, dtTransactionDate, type,
                                                parameter, amount);
        }

        /// <summary>
        /// Remove a cash transaction
        /// </summary>
        public void RemoveTransaction(UserAccountToken userToken, int transactionID)
        {
            Contract.Requires(userToken != null);

            logger.Log(userToken, LogLevel.Info, $"removing cash transaction: {transactionID}");
            _cashAccountData.RemoveCashAccountTransaction(userToken, transactionID);
        }

        public bool ValidateCashAccount(UserAccountToken userToken, DateTime dtValuationDate)
        {
            Contract.Requires(userToken != null);

            logger.Log(userToken, LogLevel.Info, "validating cash account for valuation date {0}", dtValuationDate);

            var receipts = new List<ReceiptTransaction>();
            _GetTransactionsImpl<ReceiptTransaction>(userToken, dtValuationDate, ReceiptMnemomic,
                                                        receipts);

            var payments = new List<PaymentTransaction>();
            _GetTransactionsImpl<PaymentTransaction>(userToken, dtValuationDate, PaymentMnemomic,
                                                        payments);

            var receiptTotal = receipts.Sum(x => x.Amount);
            var paymentsTotal = payments.Sum(x => x.Amount);

            if (_Match(receiptTotal, paymentsTotal) == false)
            {
                logger.Log(userToken, LogLevel.Error, "cash account validation failed!. receipts {0}, payments {1}", receiptTotal, paymentsTotal);
                return false;
            }

            logger.Log(userToken, LogLevel.Info, "cash account validation succeded!");
            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Retruns the transaction for the speciifed type (receipt or payment)
        /// </summary>
        private void _GetTransactionsImpl<T>(UserAccountToken userToken, DateTime dtValuationDate, string mnenomic,
             IList<T> transactions) where T : CashTransaction, new()
        {
            _cashAccountData.GetCashAccountTransactions(userToken, mnenomic, dtValuationDate, (reader) =>
            {
                var transaction = new T();
                transaction.TransactionID = (int)reader["TransactionID"];
                transaction.ValuationDate = dtValuationDate;
                transaction.TransactionDate = ((DateTime)reader["TransactionDate"]).ToString("yyyy-MM-dd");
                transaction.Parameter = (string)reader["Parameter"];
                transaction.TransactionType = (string)reader["TransactionType"];

                var amount = (double)reader["Amount"];
                transaction.Amount = amount;

                string transactionProperty;
                if (_transactionLookup[mnenomic].TryGetValue(transaction.TransactionType, out transactionProperty))
                {
                    //map the transaction type to the transaction property
                    var propInfo = transaction.GetType().GetProperty(transactionProperty);
                    if (propInfo != null)
                    {
                        propInfo.SetValue(transaction, amount);
                    }
                    transactions.Add(transaction);
                }
            });
        }

        private double _AddTotalRow<T>(DateTime dtValuationDate, IList<T> transactions) where T : CashTransaction, new()
        {
            //add a totals row at the bottom
            //ReceiptTransaction total = new ReceiptTransaction();
            var props = typeof(T).GetProperties();
            T total = transactions.Aggregate(new T(), (agg, transaction) =>
            {
                foreach(var prop in props)
                {
                    if(prop.PropertyType.Name == "Double")
                    {
                        double dVal = (double)prop.GetValue(agg) + (double)prop.GetValue(transaction);
                        prop.SetValue(agg, dVal);
                    }
                }
                return agg;
            });
            total.Parameter = TOTAL;
            total.IsTotal = true;
            total.TransactionDate = dtValuationDate.ToString("yyyy-MM-dd");
            transactions.Add(total);
            return total.Amount;
            //return props.Where(x => x.PropertyType.Name == "Double").Sum(x => (double)x.GetValue(total)); //total.Withdrawls + total.Other + total.Purchases;
        }


        //just compare to the third decimal place
        private static bool _Match(double d1, double d2)
        {
            return Math.Abs(Math.Round(d1,2) - Math.Round(d2,2)) <= double.Epsilon;
        }

        #endregion

        #region Public Static strings

        public static readonly string SUBSCRIPTION = "Subscription";
        public static readonly string BALANCEINHAND = "BalanceInHand";
        public static readonly string SALE = "Sale";
        public static readonly string DIVIDEND = "Dividend";
        public static readonly string INTEREST = "Interest";
        public static readonly string OTHER = "Other";
        public static readonly string ADMINFEE = "Admin Fee";
        public static readonly string PURCHASE = "Purchase";
        public static readonly string PURCHASES = "Purchases";
        public static readonly string REDEMPTION = "Redemption";
        public static readonly string WITHDRAWLS = "Withdrawls";
        public static readonly string BALANCEINHANDCF = "BalanceInHandCF";
        public static readonly string TOTAL = "TOTAL";

        #endregion

        #region Private Data

        private readonly ICashAccountInterface _cashAccountData;

        private static InvestmentBuilderLogger logger = new InvestmentBuilderLogger(LogManager.GetCurrentClassLogger());

        //this structure maps a transaction type onto its transaction property
        private readonly Dictionary<string, string> _receiptTransactionLookup =
                new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase) {
                    {DIVIDEND, DIVIDEND},
                    {SUBSCRIPTION, SUBSCRIPTION},
                    {BALANCEINHAND, SUBSCRIPTION},
                    {SALE, SALE},
                    {INTEREST, OTHER}
                };

        private readonly Dictionary<string, string> _paymentTransactionLookup =
                new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase) {
                {ADMINFEE, OTHER},
                {PURCHASE, PURCHASES},
                {REDEMPTION, WITHDRAWLS},
                {BALANCEINHANDCF, OTHER}
            };

        private Dictionary<string, Dictionary<string, string>> _transactionLookup;

        #endregion

    }
}
