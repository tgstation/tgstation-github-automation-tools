using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <summary>
	/// <see cref="IIOManager"/> that resolves paths to <see cref="Environment.CurrentDirectory"/>
	/// </summary>
	class DefaultIOManager : IIOManager
	{
		/// <summary>
		/// Default <see cref="FileStream"/> buffer size used by .NET
		/// </summary>
		public const int DefaultBufferSize = 4096;

		/// <summary>
		/// Recursively empty a directory
		/// </summary>
		/// <param name="dir"><see cref="DirectoryInfo"/> of the directory to empty</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		static async Task NormalizeAndDelete(DirectoryInfo dir, CancellationToken cancellationToken)
		{
			var tasks = new List<Task>();

			foreach (var subDir in dir.EnumerateDirectories())
			{
				cancellationToken.ThrowIfCancellationRequested();
				tasks.Add(NormalizeAndDelete(subDir, cancellationToken));
			}
			foreach (var file in dir.EnumerateFiles())
			{
				cancellationToken.ThrowIfCancellationRequested();
				file.Attributes = FileAttributes.Normal;
				file.Delete();
			}
			cancellationToken.ThrowIfCancellationRequested();
			await Task.WhenAll(tasks).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			dir.Delete(true);
		}

		/// <inheritdoc />
		public async Task AppendAllText(string path, string additional_contents, CancellationToken cancellationToken)
		{
			if (additional_contents == null)
				throw new ArgumentNullException(nameof(additional_contents));
			using (var destStream = new FileStream(ResolvePath(path), FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, DefaultBufferSize, true))
			{
				var buf = Encoding.UTF8.GetBytes(additional_contents);
				await destStream.WriteAsync(buf, 0, buf.Length, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public string ConcatPath(params string[] paths)
		{
			if (paths == null)
				throw new ArgumentNullException(nameof(paths));
			return Path.Combine(paths);
		}

		/// <inheritdoc />
		public async Task CopyFile(string src, string dest, CancellationToken cancellationToken)
		{
			if (src == null)
				throw new ArgumentNullException(nameof(src));
			if (dest == null)
				throw new ArgumentNullException(nameof(dest));
			using (var srcStream = new FileStream(ResolvePath(src), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, DefaultBufferSize, true))
			using (var destStream = new FileStream(ResolvePath(dest), FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, DefaultBufferSize, true))
				await srcStream.CopyToAsync(destStream, DefaultBufferSize, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task<List<string>> GetFilesInDirectory(string path, string searchPattern, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() =>
			{
				path = ResolvePath(path);
				var dirInfo = new DirectoryInfo(path);
				return dirInfo.GetFiles(searchPattern ?? "*").Select(x => x.FullName).ToList();
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}

		/// <inheritdoc />
		public Task CreateDirectory(string path, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() => Directory.CreateDirectory(ResolvePath(path)), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}

		/// <inheritdoc />
		public async Task DeleteDirectory(string path, CancellationToken cancellationToken)
		{
			path = ResolvePath(path);
			var di = new DirectoryInfo(path);
			if (!di.Exists)
				return;
			await NormalizeAndDelete(di, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task DeleteFile(string path, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() => File.Delete(ResolvePath(path)), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}

		/// <inheritdoc />
		public Task<bool> FileExists(string path, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() => File.Exists(ResolvePath(path)), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}

		/// <inheritdoc />
		public Task<bool> DirectoryExists(string path, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() => Directory.Exists(ResolvePath(path)), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}

		/// <inheritdoc />
		public string GetDirectoryName(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			return Path.GetDirectoryName(path);
		}

		/// <inheritdoc />
		public string GetFileName(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			return Path.GetFileName(path);
		}

		/// <inheritdoc />
		public string GetFileNameWithoutExtension(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			return Path.GetFileNameWithoutExtension(path);
		}

		/// <inheritdoc />
		public async Task<byte[]> ReadAllBytes(string path, CancellationToken cancellationToken)
		{
			path = ResolvePath(path);
			using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, DefaultBufferSize, true))
			{
				byte[] buf;
				if (file.Length >= Int32.MaxValue)
				{
					var readsRequired = (file.Length / Int32.MaxValue) + (file.Length % Int32.MaxValue == 0 ? 0 : 1);
					var collection = new List<byte[]>();

					for (var I = 0; I < readsRequired; ++I)
					{
						cancellationToken.ThrowIfCancellationRequested();
						var len = (int)(I == (readsRequired - 1) ? file.Length % Int32.MaxValue : Int32.MaxValue);
						buf = new byte[len];
						await file.ReadAsync(buf, 0, len, cancellationToken).ConfigureAwait(false);
						collection.Add(buf);
					}
					return collection.SelectMany(x => x).ToArray();
				}
				buf = new byte[file.Length];
				await file.ReadAsync(buf, 0, (int)file.Length, cancellationToken).ConfigureAwait(false);
				return buf;
			}
		}

		/// <inheritdoc />
		public virtual string ResolvePath(string path)
		{
			return Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path)));
		}

		/// <inheritdoc />
		public async Task WriteAllBytes(string path, byte[] contents, CancellationToken cancellationToken)
		{
			path = ResolvePath(path);
			using (var file = File.Open(path, FileMode.Create, FileAccess.Write))
				await file.WriteAsync(contents, 0, contents.Length, cancellationToken).ConfigureAwait(false);
		}
	}
}