using System.IO;
using System.Xml.Serialization;

namespace InvestmentBuilderCore
{
    /// <summary>
    /// Generic helper class for loading an xml configuration file. File can optionally be
    /// encrypted, in which case it should have an extension of .enc.
    /// </summary>
    public static class XmlConfigFileLoader
    {
        #region Public Methods

        /// <summary>
        /// Load an xml configuration file (optionally encrypted).
        /// </summary>
        public static T LoadConfiguration<T>(string filename, string certificate)
        {
            if(string.IsNullOrEmpty(filename))
            {
                throw new FileNotFoundException("Must specify a valid configuration file.");
            }

            //if (Path.GetExtension(filename) == ".enc")
            //{
            //    //Configuration file is encoded.
            //    //var decrypted = DataEncryption.Encryption.DecryptData(File.ReadAllBytes(filename), certificate);
            //    //using (var stream = new MemoryStream(decrypted))
            //    //{
            //    //    return LoadFromStream<T>(stream);
            //    //}
            //}
            //else
            //{
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    return LoadFromStream<T>(fs);
                }
            //}
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load the configuration from an xml stream.
        /// </summary>
        private static T LoadFromStream<T>(Stream stream)
        {
            XmlSerializer serialiser = new XmlSerializer(typeof(T));
            return (T)serialiser.Deserialize(stream);
        }

        #endregion

    }
}
