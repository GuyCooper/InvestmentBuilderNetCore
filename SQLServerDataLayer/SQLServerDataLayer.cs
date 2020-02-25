using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvestmentBuilderCore;
using System.Data.SqlClient;
using NLog;

namespace SQLServerDataLayer
{
    /// <summary>
    /// Base class for all SQL Server data layer implementations. Contains the SQL connection string and
    /// a number of helper functions.
    /// </summary>
    public class SQLServerBase
    {
        #region Public Properties

        //SQL Server connection string
        public string ConnectionStr { get; set; }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Generic method to return a value from an SQLServerreader.
        /// </summary>
        protected T GetDBValue<T>(string name, SqlDataReader reader, T defaultVal = default(T))
        {
            var result = reader[name];
            if (result.GetType() != typeof(System.DBNull))
            {
                return (T)result;
            }

            return defaultVal;
        }

        /// <summary>
        /// Method opens a connection to the sql server database. Implementation uses a connection 
        /// pooling technique so is not an expensive operation.
        /// </summary>
        protected SqlConnection OpenConnection()
        {
            try
            {
                var connection = new SqlConnection(ConnectionStr);
                connection.Open();
                return connection;
            }
            catch(Exception ex)
            {
                logger.Error(ex.Message);
                throw;
            }
        }

        #endregion

        #region Private Data

        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion
    }

    /// <summary>
    /// SQL Server implementation of IDataLayer. Holds a reference to all the
    /// individual IDataLayer interfaces so they can be accessed through a common interface.
    /// </summary>
    public class SQLServerDataLayer : SQLServerBase, IDataLayer, IDisposable
    {
        #region Public Properties

        public IClientDataInterface ClientData
        {
            get { return _clientData; }
        }

        public IInvestmentRecordInterface InvestmentRecordData
        {
            get { return _investmentRecordData; }
        }

        public ICashAccountInterface CashAccountData
        {
            get { return _cashAccountData; }
        }

        public IUserAccountInterface UserAccountData
        {
            get { return _userAccountData; }
        }

        public IHistoricalDataReader HistoricalData
        {
            get { return _historicalData; }
        }

        #endregion

        #region Public Methods

        public SQLServerDataLayer(IConfigurationSettings settings)
        {
            ConnectionStr = settings.DatasourceString;

            logger.Info($"creating SQLServerDataLayer with connection string {ConnectionStr}");

            //Connection = new SqlConnection(settings.DatasourceString);
            //Connection.Open();
            _cashAccountData = new SQLServerCashAccountData(ConnectionStr);
            _clientData = new SQLServerClientData(ConnectionStr);
            _investmentRecordData = new SQLServerInvestmentRecordData(ConnectionStr);
            _userAccountData = new SQLServerUserAccountData(ConnectionStr);
            _historicalData = new SQLServerHistoricalData(ConnectionStr);
        }

        public void ConnectNewDatasource(string datasource)
        {
            ConnectionStr = datasource;

            logger.Info($"calling ConnectNewDatasource with connectionstr {ConnectionStr}");
            //Connection.Close();
            //Connection = new SqlConnection(datasource);
            //Connection.Open();
            _clientData.ConnectionStr = ConnectionStr;
            _cashAccountData.ConnectionStr = ConnectionStr;
            _investmentRecordData.ConnectionStr = ConnectionStr;
            _userAccountData.ConnectionStr = ConnectionStr;
            _historicalData.ConnectionStr = ConnectionStr;
        }

        public void Dispose()
        {
            //Connection.Close();
        }

        #endregion

        #region Private Data Members

        private SQLServerCashAccountData _cashAccountData;
        private SQLServerClientData _clientData;
        private SQLServerInvestmentRecordData _investmentRecordData;
        private SQLServerUserAccountData _userAccountData;
        private SQLServerHistoricalData _historicalData;

        private static Logger logger = LogManager.GetCurrentClassLogger();
        #endregion
    }
}
