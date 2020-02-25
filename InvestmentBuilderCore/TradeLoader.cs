using System;
using System.Xml.Serialization;
using System.IO;

namespace InvestmentBuilderCore
{
    [XmlType("stock")]
    public class Stock
    {
        public Stock()
        {
        }

        public Stock(Stock toCopy)
        {
            if (toCopy != null)
            {
                Name = toCopy.Name;
                TransactionDate = toCopy.TransactionDate;
                Symbol = toCopy.Symbol;
                Exchange = toCopy.Exchange;
                Currency = toCopy.Currency;
                Quantity = toCopy.Quantity;
                TotalCost = toCopy.TotalCost;
                ScalingFactor = toCopy.ScalingFactor;
            }
        }

        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("date")]
        //public string TransactionDate { get; set; }
        public DateTime? TransactionDate { get; set; }
        [XmlElement("symbol")]
        public string Symbol { get; set; }
        [XmlElement("exchange")]
        public string Exchange { get; set; }
        [XmlElement("currency")]
        public string Currency { get; set; }
        [XmlElement("number")]
        public double Quantity { get; set; }
        [XmlElement("totalcost")]
        public double TotalCost { get; set; }
        [XmlElement("scaling")]
        public double ScalingFactor { get; set; }
    }

    [XmlRoot(ElementName="trades")]
    public class Trades
    {
        [XmlArray("buy")]
        public Stock[] Buys;
        [XmlArray("sell")]
        public Stock[] Sells;
        [XmlArray("changed")]
        public Stock[] Changed;
    }

    //loads the trades xml file data 
    public static class TradeLoader
    {
        static public Trades GetTrades(string tradefile)
        {
            if (File.Exists(tradefile))
            {
                using (var fs = new FileStream(tradefile, FileMode.Open))
                {
                    XmlSerializer serialiser = new XmlSerializer(typeof(Trades));
                    return (Trades)serialiser.Deserialize(fs);
                }
            }
            return new Trades();
        }

        static public void SaveTrades(Trades trades, string tradefile)
        {
            if(File.Exists(tradefile))
            {
                File.Delete(tradefile);
            }

            using (var fs = new FileStream(tradefile, FileMode.Create))
            {
                XmlSerializer serialiser = new XmlSerializer(typeof(Trades));
                serialiser.Serialize(fs, trades);
            }
        }
    }
}
