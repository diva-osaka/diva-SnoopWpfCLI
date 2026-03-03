using Xunit;

namespace SnoopWpfCLI.Tests.Integration;

[CollectionDefinition("TestApp")]
public class TestAppCollection : ICollectionFixture<TestAppFixture>
{
}
