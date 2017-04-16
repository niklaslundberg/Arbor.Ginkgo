using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Ginkgo
{
    public class IisExpress : IDisposable
    {
        private bool _isDisposed;

        private Process _process;
        private int? _processId;
        private bool _removeSiteOnExit;
        private Path _tempTemplateFilePath;

        public int? ProcessId
        {
            get
            {
                try
                {
                    return _process?.Id ?? _processId;
                }
                catch (Exception)
                {
                    return _processId;
                }
            }
        }

        public int Port { get; private set; }
        public int HttpsPort { get; private set; }

        public Path WebsitePath { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task StartAsync(Path configFile, int httpPort, int httpsPort, Path websitePath,
            bool removeSiteOnExit = false, string customHostName = "",
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null)
        {
            if (httpPort == httpsPort)
            {
                throw new ArgumentException($"HTTP port and HTTPS port cannot be the same, {httpPort}");
            }

            _removeSiteOnExit = removeSiteOnExit;
            WebsitePath = websitePath;

            _tempTemplateFilePath = TempFilePathForAppHostConfig(httpPort);

            Path iisExpressPath = DetermineIisExpressDir();

            await CreateTempAppHostConfigAsync(
                websitePath,
                configFile,
                httpPort,
                httpsPort,
                _tempTemplateFilePath,
                iisExpressPath,
                customHostName);

            string arguments = string.Format(
                CultureInfo.InvariantCulture, "/config:\"{0}\" /site:{1}", _tempTemplateFilePath,
                $"Arbor_Ginkgo_{httpPort}");

            var startInfo = new ProcessStartInfo(iisExpressPath + @"\iisexpress.exe")
            {
                WindowStyle = ProcessWindowStyle.Normal,
                ErrorDialog = true,
                LoadUserProfile = true,
                CreateNoWindow = true,
                Arguments = arguments,
                UseShellExecute = false,
            };

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
                {
                    startInfo.EnvironmentVariables.Add(environmentVariable.Key, environmentVariable.Value);
                }
            }

            var startThread = new Thread(async () => await StartIisExpressAsync(startInfo))
            {
                IsBackground = true
            };

            startThread.Start();
        }

        private Task CreateTempAppHostConfigAsync(Path websitePath, Path templateConfigFilePath, int httpPort,
            int httpsPort, Path tempFilePath, Path iisExpressPath, string customHostName = "")
        {
            var fileInfo = new FileInfo(tempFilePath.FullName);

            if (!Directory.Exists(fileInfo.Directory.FullName))
            {
                Directory.CreateDirectory(fileInfo.Directory.FullName);
            }

            File.Copy(templateConfigFilePath.FullName, tempFilePath.FullName, true);

            if (!websitePath.Exists)
            {
                throw new InvalidOperationException("The web site path is null or empty");
            }

            AddSiteToTempApphostConfig(httpPort, httpsPort, tempFilePath, websitePath, iisExpressPath, customHostName);

            return Task.FromResult(0);
        }

        private static Path TempFilePathForAppHostConfig(int port)
        {
            string tempPath = System.IO.Path.GetTempPath();

            Path tempFilePath = Path.Combine(
                tempPath,
                "Arbor.Ginkgo",
                "IntegrationTests",
                DateTime.UtcNow.Ticks.ToString(),
                port.ToString(CultureInfo.InvariantCulture),
                "applicationhost.config");

            return tempFilePath;
        }

        private static Path GetAppCmdPath(Path iisExpressPath)
        {
            return Path.Combine(iisExpressPath, "appcmd.exe");
        }

        private void AddSiteToTempApphostConfig(int httpPort, int httpsPort, Path tempFilePath, Path tempPath,
            Path iisExpressPath, string customHostName = "")
        {
            var sb = new StringBuilder();

            sb.AppendLine("Using: ");
            sb.AppendLine($"HTTP port: {httpPort.ToString(CultureInfo.InvariantCulture)}");

            if (httpsPort >= 0)
            {
                sb.AppendLine($"HTTPS port: {httpsPort.ToString(CultureInfo.InvariantCulture)}");
            }

            sb.AppendLine($"Temp file: {tempFilePath}");
            sb.AppendLine($"Temp path: {tempPath}");
            sb.AppendLine($"IIS Express path: {iisExpressPath}");
            bool hasCustomHostName = !string.IsNullOrWhiteSpace(customHostName) &&
                                     !customHostName.Equals("localhost", StringComparison.InvariantCultureIgnoreCase);

            if (hasCustomHostName)
            {
                sb.AppendLine($"Custom host name {customHostName}");
            }

            Console.WriteLine(sb.ToString());

            const string name = "Arbor_Ginkgo";

            Port = httpPort;
            HttpsPort = httpsPort;

            Console.WriteLine("Setting up new IIS Express instance on port {0} with configuration file '{1}", Port,
                tempFilePath);

            var commands = new List<string>();

            string siteName = name + "_" + httpPort.ToString(CultureInfo.InvariantCulture);
            string siteId = httpPort.ToString(CultureInfo.InvariantCulture);

            commands.Add(
                $@"set config -section:system.applicationHost/sites /+""[name='{siteName}',id='{siteId}']"" /commit:apphost /AppHostConfig:""{tempFilePath}""");

            if (hasCustomHostName)
            {
                commands.Add(
                    $@"set config -section:system.applicationHost/sites /+""[name='{siteName}',id='{siteId}'].bindings.[protocol='http',bindingInformation='*:{httpPort
                        .ToString(CultureInfo.InvariantCulture)}:{customHostName}']"" /commit:apphost /AppHostConfig:""{tempFilePath}""");

                if (httpsPort >= 0)
                {
                    commands.Add(
                        $@"set config -section:system.applicationHost/sites /+""[name='{siteName}',id='{siteId}'].bindings.[protocol='https',bindingInformation='*:{httpsPort
                            .ToString(CultureInfo.InvariantCulture)}:{customHostName}']"" /commit:apphost /AppHostConfig:""{tempFilePath}""");
                }
            }
            else
            {
                commands.Add(
                    $@"set config -section:system.applicationHost/sites /+""[name='{siteName}',id='{siteId}'].bindings.[protocol='http',bindingInformation='*:{httpPort
                        .ToString(CultureInfo.InvariantCulture)}:localhost']"" /commit:apphost /AppHostConfig:""{tempFilePath}""");


                if (httpsPort >= 0)
                {
                    commands.Add(
                        $@"set config -section:system.applicationHost/sites /+""[name='{siteName}',id='{siteId}'].bindings.[protocol='https',bindingInformation='*:{httpsPort
                            .ToString(CultureInfo.InvariantCulture)}:localhost']"" /commit:apphost /AppHostConfig:""{tempFilePath}""");
                }
            }

            commands.Add(
                $@"set config -section:system.applicationHost/sites /+""[name='{siteName}',id='{siteId}'].[path='/',applicationPool='Clr4IntegratedAppPool']"" /commit:apphost /AppHostConfig:""{tempFilePath}""");
            commands.Add(
                $@"set config -section:system.applicationHost/sites /+""[name='{siteName}',id='{siteId}'].[path='/'].[path='/',physicalPath='{tempPath}']"" /commit:apphost /AppHostConfig:""{tempFilePath}""");

            string exePath = GetAppCmdPath(iisExpressPath).FullName;

            foreach (string command in commands)
            {
                string processInfo = $"'{exePath}' {command}";

                Console.WriteLine("Executing {0}{1}", Environment.NewLine, processInfo);
                Console.WriteLine();

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo(exePath)
                    {
                        Arguments = command,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        Console.WriteLine(args.Data);
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        Console.Error.WriteLine(args.Data);
                    }
                };
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(string.Format("The process {1}{0}{1} exited with code {2}",
                        processInfo, Environment.NewLine, process.ExitCode));
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            int waitCounter = 1;

            while (!_processId.HasValue)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                Console.WriteLine("Waiting {0} milliseconds", 50 * waitCounter);
                waitCounter++;
            }

            Console.WriteLine("Disposing IIS Express process");

            if (disposing)
            {
                int? pid;

                if (_process != null)
                {
                    pid = _process.Id;
                    Console.WriteLine("Closed IIS Express");
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                    }

                    using (_process)

                    {
                        Console.WriteLine("Disposed IIS Express");
                    }
                    {
                    }
                }
                else
                {
                    pid = _processId;
                    Console.WriteLine("Process is null");
                }

                if (pid != null)
                {
                    try
                    {
                        Process process = Process.GetProcesses().SingleOrDefault(p => p.Id == pid.Value);
                        if (process != null)
                        {
                            using (process)
                            {
                                Console.WriteLine("Killing IIS Express");
                                if (!process.HasExited)
                                {
                                    process.Kill();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Could not kill process with id {0}, {1}", pid, ex);
                    }
                }
                else
                {
                    Console.WriteLine("Could not find any process id to kill");
                }
            }

            if (_removeSiteOnExit)
            {
                try
                {
                    if (WebsitePath.Exists)
                    {
                        var directoryInfo = new DirectoryInfo(WebsitePath.FullName);

                        directoryInfo.Delete(true);
                    }
                }
                catch (IOException ex)
                {
                    throw new IOException($"Could not delete the website path \'{WebsitePath.FullName}\'", ex);
                }

                if (File.Exists(_tempTemplateFilePath.FullName))
                {
                    File.Delete(_tempTemplateFilePath.FullName);
                }
            }

            _process = null;
            _processId = null;
            _isDisposed = true;
        }

        private static Path DetermineIisExpressDir()
        {
            const Environment.SpecialFolder programFiles = Environment.SpecialFolder.ProgramFilesX86;

            Path iisExpressPath = Path.Combine(Environment.GetFolderPath(programFiles), @"IIS Express");

            return iisExpressPath;
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Required here to ensure that the instance is disposed.")]
        private Task StartIisExpressAsync(ProcessStartInfo info)
        {
            try
            {
                Console.WriteLine("Starting IIS Express");
                _process = Process.Start(info);

                if (_process != null)
                {
                    _processId = _process.Id;
                    Console.WriteLine("Running IIS Express with id {0}, waiting for exit", _processId);

                    if (!_process.HasExited)
                    {
                        _process.WaitForExit();
                    }
                }
                else
                {
                    Console.WriteLine("Could not get any process reference");
                }
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("The IIS Express thread was aborted");
                Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception when running IIS Express");
                Console.WriteLine(exception);
                Dispose();
            }

            return Task.FromResult(0);
        }
    }
}