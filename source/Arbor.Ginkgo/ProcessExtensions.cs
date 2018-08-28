using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace Arbor.Ginkgo
{
    public static class ProcessExtensions
    {
        internal static void TryKillProcess(this Process process, Action<string> logger)
        {
            if (process == null)
            {
                return;
            }

            try
            {
                if (process.HasExited)
                {
                    return;
                }

                logger?.Invoke("Killing process " + process.ExecutablePath());

                process.Kill();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        internal static string ExecutablePath(this Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
                var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementBaseObject item in searcher.Get())
                {
                    object idObject = item["ProcessID"];
                    if (!(idObject is int id))
                    {
                        id = int.Parse(idObject.ToString());
                    }

                    object path = item["ExecutablePath"];

                    if (path != null && id == process.Id)
                    {
                        return path.ToString();
                    }
                }
            }

            return string.Empty;
        }

        public static void KillAllProcessRunningFromPath(string basePath, Action<string> logger = null)
        {
            List<Process> processesToKill = Process.GetProcesses()
                .Where(process => ShouldKillProcess(process, basePath))
                .ToList();

            foreach (Process process in processesToKill)
            {
                TryKillProcess(process, logger);
            }
        }

        private static bool ShouldKillProcess(this Process process, string basePath)
        {
            if (process == null)
            {
                return false;
            }

            try
            {
                if (process.HasExited)
                {
                    return false;
                }

                string processPath = process.ExecutablePath();

                if (processPath.IndexOf(basePath, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}