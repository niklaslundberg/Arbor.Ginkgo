using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                throw new ArgumentNullException(nameof(path));
            }

            string normalizePath = path.Replace('/', '\\');

            return normalizePath;
        }

        public static int CopyTo(
            this DirectoryInfo sourceDirectory,
            DirectoryInfo destinationDirectory,
            bool copySubDirectories = true,
            IEnumerable<Predicate<FileInfo>> filesToExclude = null,
            IEnumerable<string> directoriesToExclude = null)
        {
            int copiedItems = 0;

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

            List<Predicate<FileInfo>> filePredicates = filesToExclude?.ToList() ?? new List<Predicate<FileInfo>>();
            List<string> excludedDirectories = directoriesToExclude?.ToList() ?? new List<string>();

            if (destinationDirectory.Exists)
            {
                if (!destinationDirectory.IsEmpty())
                {
                    int fileCount = destinationDirectory.GetFiles().Length;
                    int directoryCount = destinationDirectory.GetDirectories().Length;
                    throw new IOException(
                        $"The directory '{destinationDirectory.FullName}' cannot be used as a target folder since there are {fileCount} files and {directoryCount} folders in the folder");
                }
            }
            else
            {
                destinationDirectory.Create();
                copiedItems++;
            }

            List<FileInfo> files = sourceDirectory.EnumerateFiles()
                .Where(file => filePredicates.All(predicate => !predicate(file))).ToList();

            foreach (FileInfo file in files)
            {
                Path tempPath = Path.Combine(destinationDirectory.FullName, file.Name);
                Debug.WriteLine($"Copying file '{file.Name}' to '{tempPath.FullName}'");
                file.CopyTo(tempPath.FullName, false);
                copiedItems++;
            }

            if (copySubDirectories)
            {
                DirectoryInfo[] subDirectories = sourceDirectory.GetDirectories();

                foreach (DirectoryInfo subDirectory in subDirectories)
                {
                    if (
                        !excludedDirectories.Any(
                            excluded => subDirectory.Name.Equals(excluded, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        Path subDirectoryTempPath = Path.Combine(destinationDirectory.FullName, subDirectory.Name);

                        var subDirectoryInfo = new DirectoryInfo(subDirectoryTempPath.FullName);

                        Debug.WriteLine(
                            $"Copying directory '{subDirectoryInfo.Name}' to '{subDirectoryTempPath.FullName}'");

                        copiedItems += subDirectory.CopyTo(subDirectoryInfo,
                            true,
                            filePredicates,
                            excludedDirectories);
                    }
                }
            }

            return copiedItems;
        }

        private static bool IsEmpty(this DirectoryInfo directoryInfo)
        {
            return !directoryInfo.EnumerateFiles().Any() && !directoryInfo.EnumerateDirectories().Any();
        }
    }
}