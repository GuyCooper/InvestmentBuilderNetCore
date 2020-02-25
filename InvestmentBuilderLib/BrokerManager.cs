using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Contracts;

namespace InvestmentBuilderLib
{
    /// <summary>
    /// Interface defines a broker
    /// </summary>
    interface IBroker
    {
        /// <summary>
        /// Name of broker
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Return the net selling value for a transaction including the broker fees
        /// </summary>
        double GetNetSellingValue(double quantity, double price);
    }

    /// <summary>
    /// IBroker contract class
    /// </summary>
    internal abstract class BrokerContract : IBroker
    {
        /// <summary>
        /// Name of broker
        /// </summary>
        public string Name
        {
            get
            {
                Contract.Ensures(string.IsNullOrEmpty(Contract.Result<string>()) == false);
                return null;
            }
        }

        /// <summary>
        /// Return the net selling value for a transaction including the broker fees
        /// </summary>
        public double GetNetSellingValue(double quantity, double price)
        {
            Contract.Requires(quantity > 0);
            Contract.Requires(price > 0);
            return 0;
        }
    }

    /// <summary>
    /// Manages available brokers
    /// </summary>
    public sealed class BrokerManager
    {
        #region Public Methods

        /// <summary>
        /// Get net selling value for a transaction by adding the brokers fees.
        /// </summary>
        public double GetNetSellingValue(string broker, double quantity, double price)
        {
            if (string.IsNullOrEmpty(broker) == false)
            {
                var result = _brokers.FirstOrDefault(x => x.Name == broker);
                if (result != null)
                {
                    return result.GetNetSellingValue(quantity, price);
                }
            }
            //if broker not specified or found just return default of value  of gross value 
            return quantity * price;
        }

        /// <summary>
        /// Returns a list of available brokers
        /// </summary>
        public IEnumerable<string> GetBrokers()
        {
            return _brokers.Select(x => x.Name);
        }

        #endregion

        #region Private Data

        private readonly IEnumerable<IBroker> _brokers = new List<IBroker>
        {
            new ShareCentreBroker(),
            new HargreavesLansdownBroker(),
            new AJBellBroker()
        };

        #endregion
    }

    /// <summary>
    /// Sharecentre broker
    /// </summary>
    class ShareCentreBroker : IBroker
    {
        /// <summary>
        /// Name of broker
        /// </summary>
        public string Name
        {
            get { return "ShareCentre"; }
        }

        /// <summary>
        /// Return the net selling value for a transaction including the broker fees
        /// </summary>
        public double GetNetSellingValue(double quantity, double price)
        {
            double dGrossValue = quantity * price;
            if (dGrossValue > 750d)
                return dGrossValue - (dGrossValue * 0.01);
            return dGrossValue - 7.5d;
        }
    }

    class HargreavesLansdownBroker : IBroker
    {
        /// <summary>
        /// Name of broker
        /// </summary>
        public string Name
        {
            get { return "HargreavesLansdown"; }
        }

        /// <summary>
        /// Return the net selling value for a transaction including the broker fees
        /// </summary>
        public double GetNetSellingValue(double quantity, double price)
        {
            double dGrossValue = quantity * price;
            return dGrossValue - 12.95d;
        }
    }

    class AJBellBroker : IBroker
    {
        /// <summary>
        /// Name of broker
        /// </summary>
        public string Name
        {
            get { return "AJBell"; }
        }
        /// <summary>
        /// Return the net selling value for a transaction including the broker fees
        /// </summary>
        public double GetNetSellingValue(double quantity, double price)
        {
            double dGrossValue = quantity * price;
            return dGrossValue - 8.95d;
        }
    }

}
