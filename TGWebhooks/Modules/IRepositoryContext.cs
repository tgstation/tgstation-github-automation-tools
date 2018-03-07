using System;
using System.Collections.Generic;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// Represents a usage of a <see cref="Octokit.Repository"/> by a <see cref="IComponentProvider"/>
	/// </summary>
	public interface IRepositoryContext : IDisposable
	{
		/// <summary>
		/// The enabled <see cref="IModule"/>s in the <see cref="IComponentProvider"/> for the <see cref="RepositoryId"/>
		/// </summary>
		IEnumerable<IModule> EnabledModules { get; }
		/// <summary>
		/// The <see cref="Octokit.Repository.Id"/>
		/// </summary>
		long RepositoryId { get; }
	}
}