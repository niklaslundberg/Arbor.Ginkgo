using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Ginkgo
{
	public class IisExpress : IDisposable
	{
		Boolean _isDisposed;

		Process _process;
		int? _processId;
		bool _removeSiteOnExit;
		Path _websitePath;

		public int? ProcessId
		{
			get
			{
				try
				{
					return _process != null ? _process.Id : _processId;
				}
				catch (Exception)
				{
					return _processId;
				}
			}
		}

		public int Port { get; private set; }

	    public Path WebsitePath
	    {
	        get { return _websitePath; }
	    }

	    public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
	    }
        
		public async Task Start(Path configFile, int port, Path websitePath, bool removeSiteOnExit = false)
		{
			_removeSiteOnExit = removeSiteOnExit;
			_websitePath = websitePath;

			var tempFilePath = TempFilePathForAppHostConfig(port);

			var iisExpressPath = DetermineIisExpressDir();

			await CreateTempAppHostConfigAsync(websitePath, configFile, port, tempFilePath, iisExpressPath);

			string arguments = String.Format(
				CultureInfo.InvariantCulture, "/config:\"{0}\" /site:{1}", tempFilePath, string.Format("Ginkgo_{0}", port));

			var info = new ProcessStartInfo(iisExpressPath + @"\iisexpress.exe")
				           {
					           WindowStyle = ProcessWindowStyle.Normal,
					           ErrorDialog = true,
					           LoadUserProfile = true,
					           CreateNoWindow = false,
					           Arguments = arguments,
					           UseShellExecute = false,
				           };

			var startThread = new Thread(async () => await StartIisExpressAsync(info))
				                  {
					                  IsBackground = true
				                  };

			startThread.Start();
		}

		async Task CreateTempAppHostConfigAsync(Path websitePath, Path templateConfigFilePath, int port, Path tempFilePath,
		                                   Path iisExpressPath)
		{
			var fileInfo = new FileInfo(tempFilePath.FullName);

			if (!Directory.Exists(fileInfo.Directory.FullName))
			{
				Directory.CreateDirectory(fileInfo.Directory.FullName);
			}

			File.Copy(templateConfigFilePath.FullName, tempFilePath.FullName, overwrite: true);

			if (!websitePath.Exists)
			{
				throw new InvalidOperationException("The web site path is null or empty");
			}

			AddSiteToTempApphostConfig(port, tempFilePath, websitePath, iisExpressPath);
		}

		static Path TempFilePathForAppHostConfig(int port)
		{
			var tempPath = System.IO.Path.GetTempPath();

			var tempFilePath = Path.Combine(tempPath, "Ginkgo", "IntegrationTests", port.ToString(),
			                                "applicationhost.config");
			return tempFilePath;
		}

		static Path GetAppCmdPath(Path iisExpressPath)
		{
			return Path.Combine(iisExpressPath, "appcmd.exe");
		}

		void AddSiteToTempApphostConfig(int port, Path tempFilePath, Path tempPath, Path iisExpressPath)
		{
			const string name = "Ginkgo";

			Port = port;

			Console.WriteLine("Setting up new IIS Express instance on port {0} with configuration file '{1}", Port, tempFilePath);

			var commands = new List<string>
				               {
					               string.Format(
						               @"set config -section:system.applicationHost/sites /+""[name='{2}_{0}',id='{0}']"" /commit:apphost /AppHostConfig:""{1}""",
						               port, tempFilePath, name),
					               string.Format(
						               @"set config -section:system.applicationHost/sites /+""[name='{2}_{0}',id='{0}'].bindings.[protocol='https',bindingInformation='*:{0}:localhost']"" /commit:apphost /AppHostConfig:""{1}""",
						               port, tempFilePath, name),
					               string.Format(
						               @"set config -section:system.applicationHost/sites /+""[name='{2}_{0}',id='{0}'].[path='/',applicationPool='Clr4IntegratedAppPool']"" /commit:apphost /AppHostConfig:""{1}""",
						               port, tempFilePath, name),
					               string.Format(
						               @"set config -section:system.applicationHost/sites /+""[name='{3}_{0}',id='{0}'].[path='/'].[path='/',physicalPath='{2}']"" /commit:apphost /AppHostConfig:""{1}""",
						               port, tempFilePath, tempPath, name),
				               };

			var exePath = GetAppCmdPath(iisExpressPath).FullName;

			foreach (var command in commands)
			{
				var process = new Process
					              {
						              StartInfo = new ProcessStartInfo(exePath)
							                          {
								                          Arguments = command,
								                          RedirectStandardOutput = true,
								                          UseShellExecute = false,
								                          RedirectStandardError = true
							                          }
					              };

				process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
				process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				process.WaitForExit();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}
		    int waitCounter = 1;

		    while(!_processId.HasValue)
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

						Console.WriteLine("Disposed IIS Express");
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
                        var process = Process.GetProcessById(pid.Value);

			            using (process)
			            {
			                Console.WriteLine("Killing IIS Express");
			                if (!process.HasExited)
			                {
			                    process.Kill();
			                }
			            }
			        }
			        catch (Exception ex)
			        {
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
					throw new IOException("Could not delete the website path '" + WebsitePath.FullName + "'", ex);
				}
			}

			_process = null;
			_processId = null;
			_isDisposed = true;
		}

		static Path DetermineIisExpressDir()
		{
			const Environment.SpecialFolder programFiles = Environment.SpecialFolder.ProgramFilesX86;

			var iisExpressPath = Path.Combine(Environment.GetFolderPath(programFiles), @"IIS Express");

			return iisExpressPath;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
			Justification = "Required here to ensure that the instance is disposed.")]
		async Task StartIisExpressAsync(ProcessStartInfo info)
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
		}
	}
}