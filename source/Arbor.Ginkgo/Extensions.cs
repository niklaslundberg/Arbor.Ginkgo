using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

namespace Arbor.Ginkgo
{
    public static class FileExtensions
    {
        public static string NormalizePath(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var normalizePath = path.Replace('/', '\\');

            return normalizePath;
        }

        public static int CopyTo(this DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory,
                                  bool copySubDirectories = true, IEnumerable<Predicate<FileInfo>> filesToExclude = null, IEnumerable<string> directoriesToExclude = null)
        {
            var copiedItems = 0;

            if (sourceDirectory == null)
            {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            if (destinationDirectory == null)
            {
                throw new ArgumentNullException(nameof(destinationDirectory));
            }

            if (!sourceDirectory.Exists)
            {
                throw new DirectoryNotFoundException(
                    $"Source directory does not exist or could not be found: {sourceDirectory.FullName}");
            }

            var filePredicates = filesToExclude ?? new List<Predicate<FileInfo>>();
            var excludedDirectories = directoriesToExclude ?? new List<string>();

            if (destinationDirectory.Exists)
            {
                if (!destinationDirectory.IsEmpty())
                {
                    var fileCount = destinationDirectory.GetFiles().Length;
                    var directoryCount = destinationDirectory.GetDirectories().Length;
                    throw new IOException(
                        $"The folder '{destinationDirectory.FullName}' cannot be used as a target folder since there are {fileCount} files and {directoryCount} folders in the folder");
                }
            }
            else
            {
                destinationDirectory.Create();
                copiedItems++;
            }

            var files = sourceDirectory.EnumerateFiles().Where(file => filePredicates.All(predicate => !predicate(file)));

            foreach (FileInfo file in files)
            {
                var temppath = Path.Combine(destinationDirectory.FullName, file.Name);
                Debug.WriteLine($"Copying file '{file.Name}' to '{temppath.FullName}'");
                file.CopyTo(temppath.FullName, false);
                copiedItems++;
            }

            if (copySubDirectories)
            {
                DirectoryInfo[] subDirectory = sourceDirectory.GetDirectories();

                foreach (DirectoryInfo subdir in subDirectory)
                {
                    if (
                        !excludedDirectories.Any(
                            excluded => subdir.Name.Equals(excluded, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var subDirectoryTempPath = Path.Combine(destinationDirectory.FullName, subdir.Name);

                        var subDirectoryInfo = new DirectoryInfo(subDirectoryTempPath.FullName);

                        Debug.WriteLine($"Copying directory '{subDirectoryInfo.Name}' to '{subDirectoryTempPath.FullName}'");

                        copiedItems += subdir.CopyTo(subDirectoryInfo);
                    }
                }
            }
            return copiedItems;
        }

        static bool IsEmpty(this DirectoryInfo directoryInfo)
        {
            return !directoryInfo.EnumerateFiles().Any() && !directoryInfo.EnumerateDirectories().Any();
        }
    }
}