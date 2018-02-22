using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// Interface for using filesystems
	/// </summary>
	public interface IIOManager
	{
		/// <summary>
		/// Retrieve the full path of some <paramref name="path"/> given a relative path. Must be used before passing relative paths to other APIs. All other operations in this <see langword="interface"/> call this internally on given paths
		/// </summary>
		/// <param name="path">Some path to retrieve the full path of</param>
		/// <returns><paramref name="path"/> as a full canonical path</returns>
		string ResolvePath(string path);

		/// <summary>
		/// Gets the directory name portion of a <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to get the file name of</param>
		/// <returns>The directory name portion of <paramref name="path"/></returns>
		string GetDirectoryName(string path);

		/// <summary>
		/// Gets the file name portion of a <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to get the file name of</param>
		/// <returns>The file name portion of <paramref name="path"/></returns>
		string GetFileName(string path);

		/// <summary>
		/// Gets the file name portion of a <paramref name="path"/> with
		/// </summary>
		/// <param name="path">The path to get the file name of</param>
		/// <returns>The file name portion of <paramref name="path"/></returns>
		string GetFileNameWithoutExtension(string path);

		/// <summary>
		/// Get a <see cref="List{T}"/> of file paths in a <paramref name="path"/> optionally matching a <paramref name="searchPattern"/>
		/// </summary>
		/// <param name="path">The path to the directory to search for files in</param>
		/// <param name="searchPattern">The search pattern for the files if any</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the list of files found optionally restricted to <paramref name="searchPattern"/></returns>
		Task<List<string>> GetFilesInDirectory(string path, string searchPattern, CancellationToken cancellationToken);

		/// <summary>
		/// Check that the directory at <paramref name="path"/> exists
		/// </summary>
		/// <param name="path">The directory to check for existence</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the file at <paramref name="path"/> exists, <see langword="false"/> otherwise</returns>
		Task<bool> DirectoryExists(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Check that the file at <paramref name="path"/> exists
		/// </summary>
		/// <param name="path">The file to check for existence</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the file at <paramref name="path"/> exists, <see langword="false"/> otherwise</returns>
		Task<bool> FileExists(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Writes some <paramref name="additionalContents"/> to a file at <paramref name="path"/> after previous content
		/// </summary>
		/// <param name="path">The path of the file to write</param>
		/// <param name="additionalContents">The contents to add to the file</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task AppendAllText(string path, string additionalContents, CancellationToken cancellationToken);

		/// <summary>
		/// Returns all the contents of a file at <paramref name="path"/> as a <see cref="byte"/> array
		/// </summary>
		/// <param name="path">The path of the file to read</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the contents of a file at <paramref name="path"/></returns>
		Task<byte[]> ReadAllBytes(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Writes some <paramref name="contents"/> to a file at <paramref name="path"/> overwriting previous content
		/// </summary>
		/// <param name="path">The path of the file to write</param>
		/// <param name="contents">The contents of the file</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task WriteAllBytes(string path, byte[] contents, CancellationToken cancellationToken);

		/// <summary>
		/// Writes some <paramref name="contents"/> to a file at <paramref name="path"/> overwriting previous content
		/// </summary>
		/// <param name="path">The path of the file to write</param>
		/// <param name="contents">The contents of the file</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task WriteAllText(string path, string contents, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes a file at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path of the file to delete</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteFile(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Copy a file from <paramref name="src"/> to <paramref name="dest"/>
		/// </summary>
		/// <param name="src">The source file to copy</param>
		/// <param name="dest">The destination path</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CopyFile(string src, string dest, CancellationToken cancellationToken);

		/// <summary>
		/// Create a directory at <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path of the directory to create</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateDirectory(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Recursively delete a directory
		/// </summary>
		/// <param name="path">The path to the directory to delete</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteDirectory(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Combines an array of strings into a path
		/// </summary>
		/// <param name="paths">The paths to combine</param>
		/// <returns>The combined path</returns>
		string ConcatPath(params string[] paths);
	}
}
