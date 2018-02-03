using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using Octokit.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Tests;

namespace TGWebhooks.MaintainerApproval.Tests
{
	[TestClass]
	public sealed class TestMaintainerApproval : TestPlugin<MaintainerApproval>
	{
		[TestMethod]
		public async Task TestInvalid()
		{
			var plugin = new MaintainerApproval();
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => plugin.EvaluateFor(null, CancellationToken.None));
		}

		[TestMethod]
		public async Task TestApprovalChecking()
		{
			var PR = new PullRequest();
			var reviews = new SimpleJsonSerializer().Deserialize<IReadOnlyList<PullRequestReview>>(TestObjects.PR1Changes1Approved);

			var plugin = await GetBasicConfigured();

			mockGitHubManager.Setup(x => x.GetPullRequestReviews(PR)).Returns(Task.FromResult(reviews));
			mockGitHubManager.Setup(x => x.UserHasWriteAccess(It.IsAny<User>())).Returns(Task.FromResult(true));
			mockGitHubManager.Setup(x => x.UserHasWriteAccess(reviews[3].User)).Returns(Task.FromResult(false));

			var result = await plugin.EvaluateFor(PR, CancellationToken.None);
			Assert.AreEqual(1, result.Progress);
			Assert.AreEqual(2, result.RequiredProgress);
		}
	}
}
