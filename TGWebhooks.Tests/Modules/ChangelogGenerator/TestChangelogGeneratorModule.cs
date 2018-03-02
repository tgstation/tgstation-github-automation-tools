using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules.Tests;

namespace TGWebhooks.Modules.Changelog.Tests
{
	/// <summary>
	/// Tests for <see cref="ChangelogModule"/>
	/// </summary>
	[TestClass]
	public sealed class TestChangelogGeneratorModule : TestModule<ChangelogModule>
	{
		protected override ChangelogModule Instantiate() => new ChangelogModule(MockDataStoreFactory.Object, MockStringLocalizer.Object, MockGeneralConfigurationOptions.Object, MockGitHubManager.Object);

		[TestMethod]
		public async Task TestEvaluate()
		{
			var clg = Instantiate();
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => clg.EvaluateFor(null, CancellationToken.None)).ConfigureAwait(false);

			var pr1 = new PullRequest(123, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, 12345, ItemState.Open, String.Empty, String.Empty, new DateTimeOffset(DateTime.UtcNow), new DateTimeOffset(), null, null, new GitReference(), new GitReference(), new User(), new User(), new List<User>(), null, null, new User(), String.Empty, 0, 1, 1, 1, 1, new Milestone(), false, new List<User>());

			MockDataStore.Setup(x => x.ReadData<RequireChangelogEntry>("12345", It.IsAny<CancellationToken>())).Returns(Task.FromResult(new RequireChangelogEntry()));

			var res = await clg.EvaluateFor(pr1, CancellationToken.None).ConfigureAwait(false);
			Assert.AreEqual(0, res.Progress);
			Assert.IsTrue(res.FailStatusReport);

			var body = ":cl: Cyberboss\ntweak: Example tweak 1\ntweak: Example tweak 2\n:cl:";

			var pr2 = new PullRequest(123, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, 12345, ItemState.Open, String.Empty, body, new DateTimeOffset(DateTime.UtcNow), new DateTimeOffset(), null, null, new GitReference(), new GitReference(), new User(), new User(), new List<User>(), null, null, new User(), String.Empty, 0, 1, 1, 1, 1, new Milestone(), false, new List<User>());

			var res2 = await clg.EvaluateFor(pr2, CancellationToken.None).ConfigureAwait(false);

			Assert.AreEqual(1, res2.Progress);
		}
	}
}
