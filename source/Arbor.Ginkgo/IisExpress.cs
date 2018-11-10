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
        private bool _ignoreSiteRemovalErrors;
        private bool _isDisposed;

        private Process _process;
        private int? _processId;
        private bool _removeSiteOnExit;
        private Path _tempTemplateFilePath;
        private Action<string> _logger;

        public IisExpress(Action<string> logger = null)
        {
            _logger = logger;
        }

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

        public bool RemoveSiteOnExit
        {
            get => _removeSiteOnExit;
            set
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(ToString());
                }

                _removeSiteOnExit = value;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return $"{nameof(Port)}: {Port}, {nameof(HttpsPort)}: {HttpsPort}, {nameof(WebsitePath)}: {WebsitePath}";
        }

        public async Task StartAsync(
            Path configFile,
            int httpPort,
            int httpsPort,
            Path websitePath,
            bool removeSiteOnExit = true,
            string customHostName = "",
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            bool ignoreSiteRemovalErrors = false)
        {
            if (httpPort == httpsPort)
            {
                throw new ArgumentException($"HTTP port and HTTPS port cannot be the same, {httpPort}");
            }

            _removeSiteOnExit = removeSiteOnExit;
            _ignoreSiteRemovalErrors = ignoreSiteRemovalErrors;
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
                CultureInfo.InvariantCulture,
                "/config:\"{0}\" /site:{1}",
                _tempTemplateFilePath,
                $"Arbor_Ginkgo_{httpPort}");

            var startInfo = new ProcessStartInfo(Path.Combine(iisExpressPath, "iisexpress.exe").FullName)
            {
                WindowStyle = ProcessWindowStyle.Normal,
                ErrorDialog = true,
                LoadUserProfile = true,
                CreateNoWindow = true,
                Arguments = arguments,
                UseShellExecute = false
            };

            if (environmentVariables != null)
            {
                foreach (KeyValuePair<string, string> environmentVariable in environmentVariables.ToArray())
                {
                    startInfo.EnvironmentVariables.Add(environmentVariable.Key, environmentVariable.Value);
                }
            }

            startInfo.WorkingDirectory = websitePath.FullName;

            var startThread = new Thread(async () => await StartIisExpressAsync(startInfo))
            {
                IsBackground = true
            };

            startThread.Start();
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
                _logger?.Invoke($"Waiting {50 * waitCounter} milliseconds");
                waitCounter++;
            }

            _logger?.Invoke("Disposing IIS Express process");

            if (disposing)
            {
                if (WebsitePath.Exists)
                {
                    ProcessExtensions.KillAllProcessRunningFromPath(WebsitePath.FullName, _logger);
                }

                int? pid;

                if (_process != null)
                {
                    pid = _process.Id;
                    _logger?.Invoke("Closed IIS Express");
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                    }

                    using (_process)

                    {
                        _logger?.Invoke("Disposed IIS Express");
                    }
                }
                else
                {
                    pid = _processId;
                    _logger?.Invoke("Process is null");
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
                                _logger?.Invoke("Killing IIS Express");
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
                    _logger?.Invoke("Could not find any process id to kill");
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
                    if (!_ignoreSiteRemovalErrors)
                    {
                        throw new IOException($"Could not delete the website path \'{WebsitePath.FullName}\'", ex);
                    }
                }
            }

            if (File.Exists(_tempTemplateFilePath.FullName))
            {
                File.Delete(_tempTemplateFilePath.FullName);
            }

            DirectoryInfo templateDirectory = new FileInfo(_tempTemplateFilePath.FullName).Directory;

            if (templateDirectory != null && templateDirectory.Exists)
            {
                templateDirectory.Delete(recursive: true);
            }

            _process = null;
            _processId = null;
            _isDisposed = true;
        }

        private static Path TempFilePathForAppHostConfig(int port)
        {
            string tempPath = System.IO.Path.GetTempPath();

            Path tempFilePath = Path.Combine(
                tempPath,
                $"Arbor.Ginkgo_{DateTime.UtcNow.Ticks}_{port.ToString(CultureInfo.InvariantCulture)}",
                "applicationhost.config");

            return tempFilePath;
        }

        private static Path GetAppCmdPath(Path iisExpressPath)
        {
            return Path.Combine(iisExpressPath, "appcmd.exe");
        }

        private static Path DetermineIisExpressDir()
        {
            const Environment.SpecialFolder programFiles = Environment.SpecialFolder.ProgramFilesX86;

            Path iisExpressPath = Path.Combine(Environment.GetFolderPath(programFiles), @"IIS Express");

            return iisExpressPath;
        }

        private Task CreateTempAppHostConfigAsync(
            Path websitePath,
            Path templateConfigFilePath,
            int httpPort,
            int httpsPort,
            Path tempFilePath,
            Path iisExpressPath,
            string customHostName = "")
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

        private void AddSiteToTempApphostConfig(
            int httpPort,
            int httpsPort,
            Path tempFilePath,
            Path tempPath,
            Path iisExpressPath,
            string customHostName = "",
            Action<string> logger = null)
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

            logger?.Invoke(sb.ToString());

            const string name = "Arbor_Ginkgo";

            Port = httpPort;
            HttpsPort = httpsPort;

            logger?.Invoke(
                $"Setting up new IIS Express instance on port {Port} with configuration file '{tempFilePath}");

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

                logger?.Invoke($"Executing {Environment.NewLine}{processInfo}");

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
                        logger?.Invoke(args.Data);
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
                        processInfo,
                        Environment.NewLine,
                        process.ExitCode));
                }
            }
        }

        [SuppressMessage("Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Required here to ensure that the instance is disposed.")]
        private Task StartIisExpressAsync(ProcessStartInfo info)
        {
            try
            {
                _logger?.Invoke("Starting IIS Express");
                _process = Process.Start(info);

                if (_process != null)
                {
                    _processId = _process.Id;
                    _logger?.Invoke($"Running IIS Express with id {_processId}, waiting for exit");

                    if (!_process.HasExited)
                    {
                        _process.WaitForExit();
                    }
                }
                else
                {
                    _logger?.Invoke("Could not get any process reference");
                }
            }
            catch (ThreadAbortException)
            {
                _logger?.Invoke("The IIS Express thread was aborted");
                Dispose();
            }
            catch (Exception exception)
            {
                _logger?.Invoke($"Exception when running IIS Express {exception}");
                Dispose();
            }

            return Task.FromResult(0);
        }
    }
}