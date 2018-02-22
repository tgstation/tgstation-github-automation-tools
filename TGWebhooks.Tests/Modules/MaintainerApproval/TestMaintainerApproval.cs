using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using Octokit.Internal;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules.Tests;
using TGWebhooks.Tests;

namespace TGWebhooks.Modules.MaintainerApproval.Tests
{
	/// <summary>
	/// Tests for <see cref="MaintainerApprovalModule"/>
	/// </summary>
	[TestClass]
	public sealed class TestMaintainerApproval : TestModule<MaintainerApprovalModule>
	{
		[TestMethod]
		public async Task TestApprovalChecking()
		{
			var PR = new PullRequest();
			var reviews = new SimpleJsonSerializer().Deserialize<IReadOnlyList<PullRequestReview>>(TestObjects.PR1Changes1Approved);

			var plugin = Instantiate();

			MockGitHubManager.Setup(x => x.GetPullRequestReviews(PR)).Returns(Task.FromResult(reviews));
			MockGitHubManager.Setup(x => x.UserHasWriteAccess(It.IsAny<User>())).Returns(Task.FromResult(true));
			MockGitHubManager.Setup(x => x.UserHasWriteAccess(reviews[3].User)).Returns(Task.FromResult(false));

			var result = await plugin.EvaluateFor(PR, CancellationToken.None).ConfigureAwait(false);
			Assert.AreEqual(1, result.Progress);
			Assert.AreEqual(2, result.RequiredProgress);
		}

		protected override MaintainerApprovalModule Instantiate()
		{
			return new MaintainerApprovalModule(MockGitHubManager.Object, MockStringLocalizer.Object);
		}
	}
}
