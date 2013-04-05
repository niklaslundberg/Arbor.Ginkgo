using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arbor.Ginkgo
{
	public static class FileExtensions
	{
		public static string NormalizePath(this string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentNullException("path");
			}

			var normalizePath = path.Replace('/', '\\');

			return normalizePath;
		}

		public static void CopyTo(this DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory,
		                          bool copySubDirectories = true, IEnumerable<Predicate<FileInfo>> filesToExclude = null)
		{
			if (sourceDirectory == null)
			{
				throw new ArgumentNullException("sourceDirectory");
			}

			if (destinationDirectory == null)
			{
				throw new ArgumentNullException("destinationDirectory");
			}

			if (!sourceDirectory.Exists)
			{
				throw new DirectoryNotFoundException(
					string.Format("Source directory does not exist or could not be found: {0}", sourceDirectory.FullName));
			}

			var filePredicates = filesToExclude ?? new List<Predicate<FileInfo>>();

			if (destinationDirectory.Exists)
			{
				if (!destinationDirectory.IsEmpty())
				{
					var fileCount = destinationDirectory.GetFiles().Length;
					var directoryCount = destinationDirectory.GetDirectories().Length;
					throw new IOException(
						string.Format(
							"The folder '{0}' cannot be used as a target folder since there are {1} files and {2} folders in the folder",
							destinationDirectory.FullName, fileCount, directoryCount));
				}
			}
			else
			{
				destinationDirectory.Create();
			}

			var files = sourceDirectory.EnumerateFiles().Where(file => filePredicates.All(predicate => !predicate(file)));

			foreach (FileInfo file in files)
			{
				var temppath = Path.Combine(destinationDirectory.FullName, file.Name);
				file.CopyTo(temppath.FullName, false);
			}

			if (copySubDirectories)
			{
				DirectoryInfo[] subDirectory = sourceDirectory.GetDirectories();

				foreach (DirectoryInfo subdir in subDirectory)
				{
					var subDirectoryTempPath = Path.Combine(destinationDirectory.FullName, subdir.Name);

					var subDirectoryInfo = new DirectoryInfo(subDirectoryTempPath.FullName);

					subdir.CopyTo(subDirectoryInfo);
				}
			}
		}

		static bool IsEmpty(this DirectoryInfo directoryInfo)
		{
			return !directoryInfo.EnumerateFiles().Any() && !directoryInfo.EnumerateDirectories().Any();
		}
	}
}