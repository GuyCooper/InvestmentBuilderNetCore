using System;
using System.Collections.Generic;
using System.Linq;

namespace InvestmentBuilderCore
{
    /// <summary>
    /// General utility methods for investment builder application.
    /// </summary>
    public static class InvestmentUtils
    {
        /// <summary>
        /// method agggregates a list of stocks into distinct aggregated stocks (i.e. if there are
        /// 2 stocks in the listwith a name of BP with amounts 5 and 3 the resulting stock list would
        /// have a single BP stock with an amount of 8 assumes all other stock members are the same
        /// </summary>
        /// <param name="stocks"></param>
        /// <returns></returns>
        public static IEnumerable<Stock> AggregateStocks(this IEnumerable<Stock> stocks)
        {
            if (stocks != null)
            {
                return stocks.Aggregate(new List<Stock>(), (a, s) =>
                {
                    var existing = a.FirstOrDefault(x => string.Compare(x.Name, s.Name) == 0);
                    if (existing == null)
                    {
                        a.Add(new Stock(s));
                    }
                    else
                    {
                        existing.Quantity += s.Quantity;
                        existing.TotalCost += s.TotalCost;
                    }
                    return a;
                });
            }
            return Enumerable.Empty<Stock>();
        }

        /// <summary>
        /// Returns true if a double is zero within max double tolerance.
        /// </summary>
        /// <returns></returns>
        public static bool IsZero(this double lhs)
        {
            return AreSame(lhs, 0d);
        }


        /// <summary>
        /// Compares the value of 2 doubles and returns true if they match within max double
        /// tolerances.
        /// </summary>
        public static bool AreSame(this double lhs, double rhs)
        {
            return Math.Abs(lhs - rhs) < double.Epsilon;
        }

        /// <summary>
        /// Extract the server name and database name from a datasource string.
        /// returns true if successful.
        /// </summary>
        public static bool extractDatabaseDetailsFromDatasource(string datasource, out string server, out string database)
        {
            server = "";
            database = "";
            //<dataSource>Data Source=DESKTOP-JJ9QOJA\SQLEXPRESS;Initial Catalog=InvestmentBuilderTest3;Integrated Security=True</dataSource>
            var mapDetails = datasource.Split(';').Select(val => val.Split('=')).Where(val => val.Length == 2).ToDictionary(val => val[0], val => val[1]);
            return mapDetails.TryGetValue("Data Source", out server) &&
                   mapDetails.TryGetValue("Initial Catalog", out database);
        }
    }
}
