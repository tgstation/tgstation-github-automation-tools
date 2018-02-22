using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TGWebhooks.Core.Tests
{
	/// <summary>
	/// Tests for <see cref="GitHubClientFactory"/>
	/// </summary>
	[TestClass]
    public sealed class TestGitHubClientFactory
	{
		[TestMethod]
		public void TestNullArgument() => Assert.ThrowsException<ArgumentNullException>(() => new GitHubClientFactory().CreateGitHubClient(null));
		[TestMethod]
		public void TestGenericCreate() => Assert.IsNotNull(new GitHubClientFactory().CreateGitHubClient("FakeToken"));
	}
}
