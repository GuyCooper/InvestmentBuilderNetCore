using System;
using System.Collections.Generic;
using RabbitTransport;
using Transports;
using Transports.Session;
using Transports.Utils;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("InvestmentBuilder Test Client");

                var settings = new ConnectionSettings("config.xml");
                using (var session = new RabbitSession(settings))
                {
                    session.RegisterMessageHandler(OnMessage);

                    RegisterTasks(session);

                    //Run tasks
                    foreach (var operation in m_operations)
                    {
                        m_currentOperation = operation;
                        m_currentOperation.DoOperation();
                    }
                }

                Console.WriteLine("Test Finished");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Test Failed:( {ex.Message}");
            }
        }

        private static void OnMessage(Message message)
        {
            m_currentOperation.ProcessResponse(message);       
        }

        private static void RegisterTasks(IConnectionSession session)
        {
            var observer = new SessionObserver();
            m_operations.Add(new LoginOperation(session, observer));
            m_operations.Add(new GetUserSessionOperation(session, observer, "Argyll Investments"));
            m_operations.Add(new GetBrokersOperation(session));
            m_operations.Add(new GetUserAccountsOperation(session));
            m_operations.Add(new ChangeAccountOperation(session));
            m_operations.Add(new GetUserSessionOperation(session, observer, "Guy SIPP"));
            m_operations.Add(new GetPortfolioOperation(session));
        }

        private static readonly List<IOperation> m_operations = new List<IOperation>();

        private static IOperation m_currentOperation;

    }
}
