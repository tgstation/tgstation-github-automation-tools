using Microsoft.VisualStudio.TestTools.UnitTesting;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Tests;

namespace TGWebhooks.TwentyFourHourRule.Tests
{
	[TestClass]
	public sealed class TestTwentyFourHourRule : TestPlugin<TwentyFourHourRule>
	{
		[TestMethod]
		public async Task TestInvalid()
		{
			var plugin = new TwentyFourHourRule();
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => plugin.EvaluateFor(null, CancellationToken.None));
		}

		[TestMethod]
		public async Task TestHours()
		{
			var plugin = await GetBasicConfigured();

			var underPR = new PullRequest(123, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, 12345, ItemState.Open, String.Empty, String.Empty, new DateTimeOffset(DateTime.UtcNow), new DateTimeOffset(), null, null, new GitReference(), new GitReference(), new User(), new User(), new List<User>(), null, new User(), String.Empty, 0, 1, 1, 1, 1, new Milestone(), false, new List<User>());
			var overPR = new PullRequest(123, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, 12345, ItemState.Open, String.Empty, String.Empty, new DateTimeOffset(DateTime.UtcNow.AddDays(-2)), new DateTimeOffset(), null, null, new GitReference(), new GitReference(), new User(), new User(), new List<User>(), null, new User(), String.Empty, 0, 1, 1, 1, 1, new Milestone(), false, new List<User>());

			var res1 = await plugin.EvaluateFor(underPR, CancellationToken.None);
			Assert.IsTrue(res1.Progress < res1.RequiredProgress);
			var res2 = await plugin.EvaluateFor(overPR, CancellationToken.None);
			Assert.IsFalse(res2.Progress < res2.RequiredProgress);
		}
	}
}
