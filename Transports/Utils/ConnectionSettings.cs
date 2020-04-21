using System.Xml;
using System.Xml.Serialization;
using InvestmentBuilderCore;

namespace Transports.Utils
{
    /// <summary>
    /// Interface defines a connection to a datasource.
    /// </summary>
    public interface IConnection
    {
        string ServerName { get; }
        string Username { get; }
        string Password { get; }
    }

    /// <summary>
    /// Interface defines connections to a server and auth server datasource.
    /// </summary>
    public interface IConnectionSettings
    {
        IConnection ServerConnection { get; }
        IConnection AuthServerConnection { get; }
    }

    /// <summary>
    /// Xml serialisable class contains settings for a datasource
    /// </summary>
    [XmlType("connection")]
    public class Connection : IConnection
    {
        [XmlElement("server")]
        public string ServerName { get; set; }
        [XmlElement("username")]
        public string Username { get; set; }
        [XmlElement("password")]
        public string Password { get; set; }
    }

    /// <summary>
    /// Xml seriaisable Class contains a connection settings to a server and auth server datasource.
    /// </summary>
    [XmlRoot(ElementName = "connections")]
    public class ConnectionSettingsImpl
    {
        [XmlElement("serverConnection")]
        public Connection ServerConnection { get; set; }
        [XmlElement("authServerConnection")]
        public Connection AuthServerConnection { get; set; }
    }

    /// <summary>
    /// Class defines connections to a server and auth server datasource.
    /// </summary>
    public class ConnectionSettings : IConnectionSettings
    {
        #region Constructor

        /// <summary>
        /// Instantiaite the connection settings
        /// </summary>
        public ConnectionSettings(string filename)
        {
            m_settings = XmlConfigFileLoader.LoadConfiguration<ConnectionSettingsImpl>(filename);
        }

        #endregion

        #region IConnectionSettings

        [XmlElement("ServerConnection")]
        public IConnection ServerConnection { get { return m_settings.ServerConnection; } }
        [XmlElement("AuthServerConnection")]
        public IConnection AuthServerConnection { get { return m_settings.AuthServerConnection; } }

        #endregion

        #region Private Data

        ConnectionSettingsImpl m_settings;

        #endregion

    }
}
