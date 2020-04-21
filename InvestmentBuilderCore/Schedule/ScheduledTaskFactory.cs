using System.Collections.Generic;
using System.Linq;

namespace InvestmentBuilderCore.Schedule
{
    /// <summary>
    /// Class registers all scheduled tasks. 
    /// </summary>
    public class ScheduledTaskFactory
    {
        #region Public Methods

        /// <summary>
        /// Get the specified task.
        /// </summary>
        public IScheduledTask GetTask(string task)
        {
            lock (m_lock)
            {
                return m_tasks.FirstOrDefault(t => t.Name == task);
            }
        }

        /// <summary>
        /// Register a task with the factory.
        /// </summary>
        public void RegisterTask(IScheduledTask task)
        {
            lock(m_lock)
            {
                m_tasks.Add(task);
            }
        }

        #endregion

        #region Private Data

        /// <summary>
        /// List of registered tasks.
        /// </summary>
        private readonly List<IScheduledTask> m_tasks = new List<IScheduledTask>();

        /// <summary>
        /// Exclusive access lock to task list
        /// </summary>
        private readonly object m_lock = new object();
        #endregion
    }
}
