﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using Octokit.Internal;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules.Tests;
using TGWebhooks.Tests;

namespace TGWebhooks.Modules.ReviewManager.Tests
{
	/// <summary>
	/// Tests for <see cref="ReviewManagerModule"/>
	/// </summary>
	[TestClass]
	public sealed class TestReviewManagerModule : TestModule<ReviewManagerModule>
	{
		[TestMethod]
		public async Task TestApprovalChecking()
		{
			var PR = new PullRequest();
			var reviews = new SimpleJsonSerializer().Deserialize<IReadOnlyList<PullRequestReview>>(TestObjects.PR1Changes1Approved);

			var plugin = Instantiate();

			MockGitHubManager.Setup(x => x.GetPullRequestReviews(PR, default)).Returns(Task.FromResult(reviews));

			var result = await plugin.EvaluateFor(PR, CancellationToken.None).ConfigureAwait(false);
			Assert.AreEqual(1, result.Progress);
			Assert.AreEqual(2, result.RequiredProgress);
		}

		protected override ReviewManagerModule Instantiate()
		{
			return new ReviewManagerModule(MockGitHubManager.Object, MockStringLocalizer.Object);
		}
	}
}
