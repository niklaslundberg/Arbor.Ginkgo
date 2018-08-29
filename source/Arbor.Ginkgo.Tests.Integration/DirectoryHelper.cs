using System;
using System.IO;
using System.Threading;

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

            int attempt = 0;
            Exception lastException = null;
            while (directoryInfo.Exists && attempt < 10)
            {
                try
                {
                    directoryInfo.Refresh();

                    if (directoryInfo.Exists)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(100));
                        directoryInfo.Delete(true);
                    }

                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;
                }
            }

            if (directoryInfo.Exists)
            {
                if (lastException != null)
                {
                    throw new InvalidOperationException($"Could not delete directory {directoryInfo.FullName}", lastException);
                }

                throw new InvalidOperationException($"Could not delete directory {directoryInfo.FullName}");
            }
        }
    }
}