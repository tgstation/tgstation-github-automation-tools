using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using TGWebhooks.Models;
using TGWebhooks.Modules.TwentyFourHourRule;

namespace TGWebhooks.Core.Tests
{
	/// <summary>
	/// Tests for <see cref="DataStoreFactory{TModule}"/>
	/// </summary>
	[TestClass]
	public sealed class TestDataStoreFactory
	{
		[TestMethod]
		public void Test()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new DataStoreFactory<TwentyFourHourRuleModule>(null));
			var mockDBContext = new Mock<IDatabaseContext>();
			var fac = new DataStoreFactory<TwentyFourHourRuleModule>(mockDBContext.Object);
			Assert.ThrowsException<ArgumentNullException>(() => fac.CreateDataStore(null));
			var mockStringLocalizer = new Mock<IStringLocalizer<TwentyFourHourRuleModule>>();
			Assert.IsNotNull(fac.CreateDataStore(new TwentyFourHourRuleModule(mockStringLocalizer.Object)));
		}
	}
}
