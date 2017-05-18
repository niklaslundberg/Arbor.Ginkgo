using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Arbor.Aesculus.Core;

namespace Arbor.Ginkgo.Tests.Integration
{
    internal class VcsTestPathHelper
    {
        public static string FindVcsRootPath()
        {
            try
            {
                Assembly ncrunchAssembly = AppDomain.CurrentDomain.Load("NCrunch.Framework");

                Type ncrunchType =
                    ncrunchAssembly.GetTypes()
                        .FirstOrDefault(
                            type => type.Name.Equals("NCrunchEnvironment", StringComparison.InvariantCultureIgnoreCase));

                if (ncrunchType != null)
                {
                    MethodInfo method = ncrunchType.GetMethod("GetOriginalSolutionPath");

                    if (method != null)
                    {
                        string originalSolutionPath = method.Invoke(null, null) as string;
                        if (!string.IsNullOrWhiteSpace(originalSolutionPath))
                        {
                            DirectoryInfo parent = new DirectoryInfo(originalSolutionPath).Parent;
                            return VcsPathHelper.FindVcsRootPath(parent.FullName);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return VcsPathHelper.FindVcsRootPath();
        }
    }
}