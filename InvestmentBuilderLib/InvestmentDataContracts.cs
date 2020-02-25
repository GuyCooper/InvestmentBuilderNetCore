using System.Diagnostics.Contracts;
using InvestmentBuilderCore;
using System;
using System.Collections.Generic;

namespace InvestmentBuilderLib
{
    [ContractClassFor(typeof(IInvestmentRecordData))]
    internal abstract class InvestmentRecordDataContract : IInvestmentRecordData
    {
        public InvestmentInformation CompanyData
        {
            get
            {
                Contract.Ensures(Contract.Result<InvestmentInformation>() != null);
                return null;
            }
        }

        public string Name
        {
            get
            {
                Contract.Ensures(string.IsNullOrEmpty(Contract.Result<string>()) == false);
                return null;
            }
        }

        public double Price
        {
            get
            {
                Contract.Ensures(Contract.Result<double>() > 0);
                return 0;
            }
        }

        public void AddNewShares(DateTime valuationDate, Stock stock)
        {
            Contract.Requires(stock != null);
        }

        public void ChangeShareHolding(DateTime valuationDate, double quantity)
        {
            Contract.Requires(quantity > 0);
        }

        public void SellShares(DateTime valuationDate, Stock stock)
        {
            Contract.Requires(stock != null);
        }

        public void UpdateClosingPrice(DateTime valuationDate, double dClosing)
        {
            throw new NotImplementedException();
        }

        public void UpdateDividend(DateTime valuationDate, double dDividend)
        {
            Contract.Requires(dDividend > 0);
        }

        public void UpdateRow(DateTime valuationDate, DateTime previousDate)
        {
            Contract.Requires(valuationDate > previousDate);
        }
    }

    [ContractClassFor(typeof(IInvestmentRecordDataManager))]
    internal abstract class InvestmentRecordDataManagerContract : IInvestmentRecordDataManager
    {
        public IEnumerable<CompanyData> GetInvestmentRecords(UserAccountToken userToken, AccountModel account, DateTime dtValuationDate, DateTime? dtPreviousValuationDate, ManualPrices manualPrices, bool bSnapshot)
        {
            Contract.Requires(userToken != null);
            Contract.Requires(account != null);
            Contract.Ensures(Contract.Result<IEnumerable<CompanyData>>() != null);
            return null;
        }

        public IEnumerable<CompanyData> GetInvestmentRecordSnapshot(UserAccountToken userToken, AccountModel account, ManualPrices manualPrices)
        {
            Contract.Requires(userToken != null);
            Contract.Requires(account != null);
            Contract.Ensures(Contract.Result<IEnumerable<CompanyData>>() != null);
            return null;
        }

        public DateTime? GetLatestRecordValuationDate(UserAccountToken userToken)
        {
            Contract.Requires(userToken != null);
            return null;
        }

        public bool UpdateInvestmentRecords(UserAccountToken userToken, AccountModel account, Trades trades, CashAccountData cashData, DateTime valuationDate, ManualPrices manualPrices, DateTime? dtPreviousValuation, ProgressCounter progress)
        {
            Contract.Requires(userToken != null);
            Contract.Requires(account != null);
            Contract.Requires(cashData != null);
            return false;
        }
    }
}
