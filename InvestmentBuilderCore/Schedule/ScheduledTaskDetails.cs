using System;
using System.Xml.Serialization;

namespace InvestmentBuilderCore.Schedule
{
    /// <summary>
    /// An individual scheduled task
    /// </summary>
    [XmlType("task")]
    public class ScheduledTaskDetails
    {
        /// <summary>
        /// Unique name for scheduled task.
        /// </summary>
        [XmlElement("name")]
        public string Name { get; set; }

        /// <summary>
        /// Schedule time for task (once a day).
        /// </summary>
        [XmlElement("scheduled")]
        public DateTime ScheduledTime { get; set; }

    }
}
