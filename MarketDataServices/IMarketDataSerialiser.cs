using System;

namespace MarketDataServices
{
    /// <summary>
    /// Abstracted interface for serialising market data.
    /// </summary>
    internal interface IMarketDataSerialiser
    {
        /// <summary>
        /// Starts a data serialiser. 
        /// </summary>
        void StartSerialiser();

        /// <summary>
        /// Close a serialiser.
        /// </summary>
        void EndSerialiser();

        /// <summary>
        /// Serialise some data.
        /// </summary>
        void SerialiseData(string data, params object[] prm);

        /// <summary>
        /// Load all the data from a serialiser. For each block of data call the processRecord
        /// delegate.
        /// </summary>
        void LoadData(Action<string> processRecord);
    }
}
