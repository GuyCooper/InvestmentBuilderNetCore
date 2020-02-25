using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace InvestmentBuilderCore
{
    public class InvestmentBuilderLogger
    {
        private Logger _logger;
        public InvestmentBuilderLogger(Logger logger)
        {
            _logger = logger;
        }

        public void Log(UserAccountToken userToken, LogLevel level, string message, params object[] args)
        {
            _logger.Log(level, () =>
            {
                var dataMessage = args != null ? string.Format(message, args) : message;
                var sb = new StringBuilder();
                sb.Append(userToken.User);
                sb.Append(";");
                sb.Append(dataMessage);
                return sb.ToString();
            });
        }
    }
}
