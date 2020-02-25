using System.IO;

namespace InvestmentBuilderCore
{
    /// <summary>
    /// Helper class for spawnig a process.
    /// </summary>
    public static class ProcessLauncher
    {
        #region Public Methods

        /// <summary>
        /// Spawn a new process and return immediatley.
        /// </summary>
        public static void RunProcess(string filename, string commandLineParams)
        {
            var process = SpawnProcess(filename, commandLineParams);
        }

        /// <summary>
        /// Spawn a new process and wait for it to complete before returning. Returns the
        /// exit code of the process.
        /// </summary>
        public static int RunProcessAndWaitForCompletion(string filename, string commandLineParams)
        {
            var process = SpawnProcess(filename, commandLineParams);
            process.WaitForExit();
            return process.ExitCode;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Spawn a process
        /// </summary>
        private static System.Diagnostics.Process SpawnProcess(string filename, string commandLineParams)
        {
            var workingFolder = Path.GetDirectoryName(filename);
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = commandLineParams;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.ErrorDialog = true;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.WorkingDirectory = workingFolder;
            process.Start();
            return process;
        }

        #endregion

    }
}
