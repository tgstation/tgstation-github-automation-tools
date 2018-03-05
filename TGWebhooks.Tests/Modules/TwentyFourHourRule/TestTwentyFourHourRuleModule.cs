using Microsoft.VisualStudio.TestTools.UnitTesting;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules.Tests;

namespace TGWebhooks.Modules.TwentyFourHourRule.Tests
{
	/// <summary>
	/// Tests for <see cref="TwentyFourHourRuleModule"/>
	/// </summary>
	[TestClass]
	public sealed class TestTwentyFourHourRuleModule : TestModule<TwentyFourHourRuleModule>
	{
		[TestMethod]
		public async Task TestInvalid()
		{
			var plugin = Instantiate();
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => plugin.EvaluateFor(null, CancellationToken.None)).ConfigureAwait(false);
		}

		[TestMethod]
		public async Task TestHours()
		{
			var plugin = Instantiate();

			var underPR = new PullRequest(123, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, 12345, ItemState.Open, String.Empty, String.Empty, new DateTimeOffset(DateTime.UtcNow), new DateTimeOffset(), null, null, new GitReference(), new GitReference(), new User(), new User(), new List<User>(), null, null, new User(), String.Empty, 0, 1, 1, 1, 1, new Milestone(), false, new List<User>());
			var overPR = new PullRequest(123, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, 12345, ItemState.Open, String.Empty, String.Empty, new DateTimeOffset(DateTime.UtcNow.AddDays(-2)), new DateTimeOffset(), null, null, new GitReference(), new GitReference(), new User(), new User(), new List<User>(), null, null, new User(), String.Empty, 0, 1, 1, 1, 1, new Milestone(), false, new List<User>());

			var res1 = await plugin.EvaluateFor(underPR, CancellationToken.None).ConfigureAwait(false);
			Assert.IsTrue(res1.Progress < res1.RequiredProgress);
			var res2 = await plugin.EvaluateFor(overPR, CancellationToken.None).ConfigureAwait(false);
			Assert.IsFalse(res2.Progress < res2.RequiredProgress);
		}

		protected override TwentyFourHourRuleModule Instantiate()
		{
			return new TwentyFourHourRuleModule(MockStringLocalizer.Object);
		}
	}
}
