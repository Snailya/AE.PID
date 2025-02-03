using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Client.Infrastructure.VisioExt.Services;
using FluentAssertions;
using Moq;

namespace AE.PID.Visio.Shared.Tests;

public class AppUpdateServiceTest
{
    private readonly AppVersionDto _appVersionDto = new()
    {
        Version = "1.0.0.0",
        DownloadUrl = "",
        ReleaseNotes = ""
    };

    [Theory]
    [InlineData("0.0.0.0", "1.0.0.0")]
    [InlineData("1.0.0.0", null)]
    public async Task GetUpdateAsyncTest(string version, string expectedVersion)
    {
        var apiMock = new Mock<IAppApi>();
        apiMock
            .Setup(api => api.GetCurrentApp())
            .ReturnsAsync(_appVersionDto);

        var apiFactoryMock = new Mock<IApiFactory<IAppApi>>();
        apiFactoryMock.Setup(x => x.Api).Returns(apiMock.Object);
        
        var configurationMock = new Mock<IConfigurationService>();

        var appUpdateService = new AppUpdateService(configurationMock.Object,apiFactoryMock.Object);

        var result = await appUpdateService.CheckUpdateAsync(version);
        result?.Version.Should().Be(expectedVersion);
    }

    [Fact]
    public async Task GetUpdateAsync_ShouldThrowNetworkNotValidException_WhenThereIsNetworkError()
    {
        var apiMock = new Mock<IAppApi>();
        apiMock
            .Setup(api => api.GetCurrentApp())
            .Throws(new HttpRequestException());

        var apiFactoryMock = new Mock<IApiFactory<IAppApi>>();
        apiFactoryMock.Setup(x => x.Api).Returns(apiMock.Object);

        var configurationMock = new Mock<IConfigurationService>();

        var appUpdateService = new AppUpdateService(configurationMock.Object,apiFactoryMock.Object);

        var action = async () => await appUpdateService.CheckUpdateAsync("0.0.0.0");
        await action.Should().ThrowAsync<NetworkNotValidException>();
    }
}