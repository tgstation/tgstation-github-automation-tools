using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;

namespace TGWebhooks.Tests
{
	[TestClass]
	public abstract class TestPlugin<TModule> where TModule : IModule, new()
	{
		protected Mock<ILogger> mockLogger;
		protected Mock<IRepository> mockRepository;
		protected Mock<IGitHubManager> mockGitHubManager;
		protected Mock<IIOManager> mockIOManager;
		protected Mock<IWebRequestManager> mockWebRequestManager;
		protected Mock<IDataStore> mockDataStore;
		protected Mock<IStringLocalizer> mockStringLocalizer;

		[TestMethod]
		public void TestInstantiation()
		{
			var plugin = new TModule();
			Assert.IsFalse(String.IsNullOrWhiteSpace(plugin.Name));
			Assert.IsFalse(String.IsNullOrWhiteSpace(plugin.Description));

			Assert.IsNotNull(plugin.Uid);
			Assert.AreEqual(plugin.Uid, plugin.Uid);
		}

		[TestMethod]
		public async Task TestConfigurationAndInitialization()
		{
			await GetBasicConfigured().ConfigureAwait(false);
		}

		protected async Task<TModule> GetBasicConfigured()
		{
			var plugin = new TModule();
			mockLogger = new Mock<ILogger>();
			mockRepository = new Mock<IRepository>();
			mockGitHubManager = new Mock<IGitHubManager>();
			mockIOManager = new Mock<IIOManager>();
			mockWebRequestManager = new Mock<IWebRequestManager>();
			mockDataStore = new Mock<IDataStore>();
			mockStringLocalizer = new Mock<IStringLocalizer>();
			plugin.Configure(mockLogger.Object, mockRepository.Object, mockGitHubManager.Object, mockIOManager.Object, mockWebRequestManager.Object, mockDataStore.Object, mockStringLocalizer.Object);

			await plugin.Initialize(CancellationToken.None).ConfigureAwait(false);

			Assert.IsNotNull(plugin.MergeRequirements);
			Assert.IsNotNull(plugin.GetPayloadHandlers<ActivityPayload>());
			return plugin;
		}
	}
}
