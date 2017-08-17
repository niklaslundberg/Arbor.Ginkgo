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
            try
            {

                foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
                {
                    DeleteRecursive(directory);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Could not delete files in directory '{directoryInfo.FullName}', {ex}");
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