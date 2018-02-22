using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Octokit;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;

namespace TGWebhooks.Modules.Tests
{
	[TestClass]
	public abstract class TestModule<TModule> where TModule : IModule
	{
		protected Mock<ILogger> MockLogger { get; private set;  }
		protected Mock<IRepository> MockRepository { get; private set; }
		protected Mock<IGitHubManager> MockGitHubManager { get; private set; }
		protected Mock<IIOManager> MockIOManager { get; private set; }
		protected Mock<IWebRequestManager> MockWebRequestManager { get; private set; }
		protected Mock<IDataStoreFactory<TModule>> MockDataStoreFactory { get; private set; }
		protected Mock<IDataStore> MockDataStore { get; private set; }
		protected Mock<IStringLocalizer<TModule>> MockStringLocalizer { get; private set; }
		protected Mock<IOptions<GeneralConfiguration>> MockGeneralConfigurationOptions { get; private set; }

		public TestModule()
		{
			MockLogger = new Mock<ILogger>();
			MockRepository = new Mock<IRepository>();
			MockGitHubManager = new Mock<IGitHubManager>();
			MockIOManager = new Mock<IIOManager>();
			MockWebRequestManager = new Mock<IWebRequestManager>();
			MockDataStoreFactory = new Mock<IDataStoreFactory<TModule>>();
			MockDataStore = new Mock<IDataStore>();
			MockDataStoreFactory.Setup(x => x.CreateDataStore(It.IsAny<TModule>())).Returns(MockDataStore.Object);
			MockStringLocalizer = new Mock<IStringLocalizer<TModule>>();
			MockGeneralConfigurationOptions = new Mock<IOptions<GeneralConfiguration>>();
			MockGeneralConfigurationOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration());
		}

		protected abstract TModule Instantiate();

		[TestMethod]
		public async Task TestModuleBasics()
		{
			var module = Instantiate();
			Assert.IsFalse(String.IsNullOrWhiteSpace(module.Name));
			Assert.IsFalse(String.IsNullOrWhiteSpace(module.Description));
			Assert.IsNotNull(module.Uid);
			Assert.AreEqual(module.Uid, module.Uid);
			await module.Initialize(CancellationToken.None).ConfigureAwait(false);

			Assert.IsNotNull(module.MergeRequirements);
			Assert.IsNotNull(module.GetPayloadHandlers<ActivityPayload>());
		}
	}
}
