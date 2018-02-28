using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;

namespace TGWebhooks.Core.Tests
{
	/// <summary>
	/// Tests for <see cref="Application"/>
	/// </summary>
	[TestClass]
	public sealed class TestApplication
	{
		[TestMethod]
		public void TestInstatiation()
		{
			var mockConfig = new Mock<IConfiguration>();
			var mockHostingEnvironment = new Mock<IHostingEnvironment>();
			mockHostingEnvironment.SetupGet(x => x.ContentRootPath).Returns(String.Empty);
			Assert.ThrowsException<ArgumentNullException>(() => new Application(null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new Application(mockConfig.Object, null));
			Assert.ThrowsException<ArgumentNullException>(() => new Application(null, mockHostingEnvironment.Object));
			var app = new Application(mockConfig.Object, mockHostingEnvironment.Object);
		}

		[TestMethod]
		public void TestConfigureServices()
		{
			var mockConfig = new Mock<IConfiguration>();
			var mockConfigSection = new Mock<IConfigurationSection>();
			mockConfig.Setup(x => x.GetSection(It.IsAny<string>())).Returns(mockConfigSection.Object);
			var mockHostingEnvironment = new Mock<IHostingEnvironment>();
			mockHostingEnvironment.SetupGet(x => x.ContentRootPath).Returns(String.Empty);
			var app = new Application(mockConfig.Object, mockHostingEnvironment.Object);
			Assert.ThrowsException<ArgumentNullException>(() => app.ConfigureServices(null));

			var mockServiceCollection = new Mock<IServiceCollection>();
			mockServiceCollection.Setup(x => x.GetEnumerator()).Returns(Enumerable.Empty<ServiceDescriptor>().GetEnumerator());

			app.ConfigureServices(mockServiceCollection.Object);
		}
	}
}
