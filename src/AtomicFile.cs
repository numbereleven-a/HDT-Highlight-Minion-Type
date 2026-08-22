using System;
using System.IO;

namespace HDTHighlightMinionType
{
	internal static class AtomicFile
	{
		public static void Write(string path, Action<Stream> writer)
		{
			if(string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A destination path is required.", nameof(path));
			if(writer == null) throw new ArgumentNullException(nameof(writer));

			var fullPath = Path.GetFullPath(path);
			var directory = Path.GetDirectoryName(fullPath);
			if(string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("The destination directory could not be determined.");
			Directory.CreateDirectory(directory);
			var temporaryPath = Path.Combine(directory, Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");

			try
			{
				using(var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
					writer(stream);
					stream.Flush(true);
				}
				if(File.Exists(fullPath)) File.Replace(temporaryPath, fullPath, null);
				else File.Move(temporaryPath, fullPath);
			}
			finally
			{
				try { if(File.Exists(temporaryPath)) File.Delete(temporaryPath); }
				catch { }
			}
		}
	}
}
