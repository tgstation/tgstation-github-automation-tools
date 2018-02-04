using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

namespace TGWebhooks.Tests
{
	[TestClass]
	public abstract class TestPlugin<TPlugin> where TPlugin : IPlugin, new()
	{
		protected Mock<ILogger> mockLogger;
		protected Mock<IRepository> mockRepository;
		protected Mock<IGitHubManager> mockGitHubManager;
		protected Mock<IIOManager> mockIOManager;
		protected Mock<IWebRequestManager> mockWebRequestManager;
		protected Mock<IDataStore> mockDataStore;

		[TestMethod]
		public void TestInstantiation()
		{
			var plugin = new TPlugin();
			Assert.IsFalse(String.IsNullOrWhiteSpace(plugin.Name));
			Assert.IsFalse(String.IsNullOrWhiteSpace(plugin.Description));

			Assert.IsFalse(plugin.Enabled);
			plugin.Enabled = true;
			Assert.IsTrue(plugin.Enabled);

			Assert.IsNotNull(plugin.Guid);
			Assert.AreEqual(plugin.Guid, plugin.Guid);
		}

		[TestMethod]
		public async Task TestConfigurationAndInitialization()
		{
			await GetBasicConfigured();
		}

		protected async Task<TPlugin> GetBasicConfigured()
		{
			var plugin = new TPlugin();
			mockLogger = new Mock<ILogger>();
			mockRepository = new Mock<IRepository>();
			mockGitHubManager = new Mock<IGitHubManager>();
			mockIOManager = new Mock<IIOManager>();
			mockWebRequestManager = new Mock<IWebRequestManager>();
			mockDataStore = new Mock<IDataStore>();
			plugin.Configure(mockLogger.Object, mockRepository.Object, mockGitHubManager.Object, mockIOManager.Object, mockWebRequestManager.Object, mockDataStore.Object);

			await plugin.Initialize(CancellationToken.None);

			Assert.IsNotNull(plugin.MergeRequirements);
			Assert.IsNotNull(plugin.GetPayloadHandlers<ActivityPayload>());
			return plugin;
		}
	}
}
