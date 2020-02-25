using System;
using Transports;

namespace InvestmentBuilderManager.Dtos
{
    /// <summary>
    /// Transaction type for a trade.
    /// </summary>
    public enum TransactionType
    {
        None,
        Buy,
        Sell,
        Change
    }

    /// <summary>
    /// Dto of a trade item.
    /// </summary>
    internal class TradeItemDto : Dto
    {
        public string Currency { get; set; }
        public string ItemName { get; set; }
        public double Quantity { get; set; }
        public string Symbol { get; set; }
        public double TotalCost { get; set; }
        public DateTime? TransactionDate { get; set; }
        public TransactionType Action { get; set; }
    }
}
