using System;
using System.Collections.Generic;
using InvestmentBuilderCore;

namespace InvestmentReportService
{
    /// <summary>
    /// Class defines the current status of a report
    /// </summary>
    internal class ReportStatus
    {
        public bool IsBuilding { get; set; }
        public int Progress { get; set; }
        public string BuildSection { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public string CompletedReport { get; set; }
    }

    /// <summary>
    /// BuildMonitor interface. Monitor class allows a build process to be
    /// monitored.
    /// </summary>
    internal interface IBuildMonitor
    {
        void StartBuilding();
        void StopBuiliding(string completedReport);
        ReportStatus GetReportStatus();
        ProgressCounter GetProgressCounter();
    }

    /// <summary>
    /// Class allows monitoring of the build report request.
    /// </summary>
    internal class BuildReportMonitor : IBuildMonitor
    {
        private string username_;
        private bool isbuilding_;
        private IEnumerable<string> errors_;
        private ProgressCounter counter_;
        private string completedReport_;

        private static Dictionary<string, List<string>> ErrorLookup = new Dictionary<string, List<string>>();

        public BuildReportMonitor(string username)
        {
            counter_ = new ProgressCounter();
            username_ = username;
            isbuilding_ = false;
        }

        public void StartBuilding()
        {
            isbuilding_ = true;
            errors_ = null;
        }

        public void StopBuiliding(string completedReport)
        {
            isbuilding_ = false;
            errors_ = _GetUserErrors();
            completedReport_ = completedReport;
        }

        public void AddError(string error)
        {
            if (errors_ == null)
            {
                errors_ = new List<string>
                {
                    error
                };
            }
        }
        public static void LogMethod(string level, string message)
        {
            //errors that are displayed in client. only include error message
            if (level.Equals("ERROR", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                int index = message.IndexOf(';');
                if (index > 0)
                {
                    string user = message.Substring(0, index);
                    string errorData = message.Substring(index + 1);

                    List<string> errorList;
                    if (ErrorLookup.TryGetValue(user, out errorList) == false)
                    {
                        errorList = new List<string>();
                        ErrorLookup.Add(user, errorList);
                    }
                    errorList.Add(errorData);
                }
            }
        }

        private IEnumerable<string> _GetUserErrors()
        {
            IEnumerable<string> result = null;
            if (ErrorLookup.ContainsKey(username_))
            {
                result = ErrorLookup[username_];
                ErrorLookup[username_].Clear();
            }
            return result;
        }

        public ReportStatus GetReportStatus()
        {
            return new ReportStatus
            {
                Progress = counter_.Count,
                BuildSection = counter_.Section,
                IsBuilding = isbuilding_,
                Errors = errors_,
                CompletedReport = completedReport_
            };
        }

        public ProgressCounter GetProgressCounter()
        {
            return counter_;
        }
    }
}
