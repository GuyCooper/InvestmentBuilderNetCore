
namespace InvestmentBuilderCore.Schedule
{
    /// <summary>
    /// Interface for a task that will be run from a schedule.
    /// </summary>
    public interface IScheduledTask
    {
        /// <summary>
        /// Name of task
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Run the task
        /// </summary>
        void RunTask();
    }
}
