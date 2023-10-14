using Xunit;

namespace UnitTesting.Infrastructure
{
	[CollectionDefinition(Name)]
	public class TestsCollection : ICollectionFixture<TestHost>
	{
		public const string Name = nameof(TestsCollection);
	}
}
