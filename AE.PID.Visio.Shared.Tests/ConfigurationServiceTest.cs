using AE.PID.Client.Core;
using AE.PID.Client.Infrastructure;
using FluentAssertions;
using Moq;

namespace AE.PID.Visio.Shared.Tests;

public class ConfigurationServiceTest
{
    [Fact]
    public void GetUpdateAsyncTest()
    {
        var storageServiceMock = new Mock<IStorageService>();
        var configurationService = new ConfigurationService(storageServiceMock.Object, "Test", "1.0.0.0");

        configurationService.UpdateProperty(x => x.Server, "http://test.com");

        var actual = configurationService.GetCurrentConfiguration();

        actual.Server.Should().Be("http://test.com");
    }
}