using System;
using System.IO;

namespace Arbor.Ginkgo.Tests.Integration
{
    public static class DirectoryHelper
    {
        public static void DeleteRecursive(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
            {
                return;
            }

            directoryInfo.Refresh();

            if (!directoryInfo.Exists)
            {
                return;
            }

            foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
            {
                DeleteRecursive(directory);
            }

            foreach (FileInfo fileInfo in directoryInfo.GetFiles())
            {
                try
                {
                    fileInfo.Delete();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not delete file '{fileInfo.FullName}', {ex}");
                    throw;
                }
            }
        }
    }
}