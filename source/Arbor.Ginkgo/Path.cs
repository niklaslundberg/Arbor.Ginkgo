using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arbor.Ginkgo
{
	public class Path
	{
		readonly List<string> _pathSegments = new List<string>();

		public Path(string fullName)
		{
			if (string.IsNullOrWhiteSpace(fullName))
			{
				throw new ArgumentNullException("fullName");
			}

			var normalizedPath = fullName.NormalizePath();

			var segments = normalizedPath.Split('\\');

			_pathSegments.AddRange(segments);
		}

		public string FullName
		{
			// ReSharper disable SpecifyACultureInStringConversionExplicitly
			get { return string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), _pathSegments); }
			// ReSharper restore SpecifyACultureInStringConversionExplicitly
		}

		public bool Exists
		{
			get
			{
				if (!_pathSegments.Any())
				{
					return false;
				}

				try
				{
					if (File.Exists(FullName))
					{
						return true;
					}
				}
				catch (FileNotFoundException)
				{
					return false;
				}
				try
				{
					if (Directory.Exists(FullName))
					{
						return true;
					}
				}
				catch (DirectoryNotFoundException)
				{
					return false;
				}
				return false;
			}
		}

		public static Path Combine(Path path1, string path2)
		{
			if (path1 == null)
			{
				throw new ArgumentNullException("path1");
			}

			if (string.IsNullOrWhiteSpace(path2))
			{
				throw new ArgumentNullException("path2");
			}

			var combined = System.IO.Path.Combine(path1.FullName, path2);

			return new Path(combined);
		}

		public static Path Combine(params string[] paths)
		{
			var combined = System.IO.Path.Combine(paths);

			return new Path(combined);
		}

		public override string ToString()
		{
			return FullName;
		}

		public static Path Combine(Path path1, params string[] paths)
		{
			if (path1 == null)
			{
				throw new ArgumentNullException("path1");
			}

			var pathList = new List<string>(paths);

			pathList.Insert(0, path1.FullName);

			var combined = System.IO.Path.Combine(pathList.ToArray());

			return new Path(combined);
		}
	}
}