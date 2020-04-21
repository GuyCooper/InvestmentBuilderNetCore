using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace InvestmentBuilderCore.Schedule
{
    /// <summary>
    /// Scheduler class.
    /// </summary>
    public class Scheduler : IDisposable
    {
        #region Private classes

        /// <summary>
        /// Defines a scheduled task. including next run time
        /// </summary>
        private class ScheduledTaskInternal
        {
            public ScheduledTaskDetails Details { get; set; }
            public DateTime? NextRunTime { get; set; }
            public DateTime? LastRunTime { get; set; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialise the scheduler.
        /// </summary>
        public Scheduler(ScheduledTaskFactory taskFactory, IEnumerable<ScheduledTaskDetails> tasksDetails) 
        {
            m_taskFactory = taskFactory;
            m_scheduledTasks = tasksDetails.Select(task => new ScheduledTaskInternal { Details = task }).ToList();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                m_closeEvent.Set();
            };
        }

        /// <summary>
        /// Run the scheduler. This method blocks until the application receives a stop.
        /// </summary>
        public void Run()
        {
            while(true)
            {
                var stop = m_closeEvent.WaitOne(1000);
                if(stop)
                {
                    return;
                }
                //Check if there are any tasks that need to be run
                var now = DateTime.UtcNow;
                DateTime nextRunTime;

                foreach (var task in m_scheduledTasks)
                {
                    if (task.NextRunTime == null)
                    {
                        var span = task.Details.ScheduledTime.TimeOfDay;
                        nextRunTime = DateTime.Today + span;
                        if(now >= nextRunTime)
                        {
                            nextRunTime = nextRunTime.AddDays(1);
                        }
                        task.NextRunTime = nextRunTime;
                    }
                    else
                    {
                        nextRunTime = task.NextRunTime.Value;
                    }
                        
                    if(now >= nextRunTime)
                    {
                        // This task is scheduled to run now
                        task.NextRunTime = nextRunTime.AddDays(1);
                        var job = m_taskFactory.GetTask(task.Details.Name);
                        if(job == null)
                        {
                            logger.Error($"Cannot find task {task.Details.Name}.");
                        }
                        else
                        {
                            logger.Info($"Running task {task.Details.Name}.");
                            task.LastRunTime = now;
                            try
                            {
                                job.RunTask();
                            }
                            catch(Exception ex)
                            {
                                logger.Error(ex);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Send a signal to stop the scheduler
        /// </summary>
        public void Stop()
        {
            m_closeEvent.Set();
            return;
        }

        public void Dispose()
        {
            m_closeEvent.Dispose();
        }

        #endregion

        #region Private Data

        private readonly ManualResetEvent m_closeEvent = new ManualResetEvent(false);

        private readonly List<ScheduledTaskInternal> m_scheduledTasks;
        private readonly ScheduledTaskFactory m_taskFactory;

        private static Logger logger = LogManager.GetCurrentClassLogger();
        #endregion
    }
}
