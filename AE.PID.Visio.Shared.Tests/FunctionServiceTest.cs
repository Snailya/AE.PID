using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Shared.Services;
using FluentAssertions;
using Moq;

namespace AE.PID.Visio.Shared.Tests;

public class FunctionServiceTest
{
    private readonly Mock<IFunctionApi> _apiMock;

    private readonly IEnumerable<FunctionDto> _groupsInProject =
    [
        new() { Id = 2 }
    ];

    private readonly IEnumerable<FunctionDto> _standards =
    [
        new() { Id = 1 }
    ];

    private readonly IEnumerable<FunctionDto> _zonesInProject =
    [
        new() { Id = 1 }
    ];

    public FunctionServiceTest()
    {
        _apiMock = new Mock<IFunctionApi>();
        _apiMock
            .Setup(api => api.GetFunctionsAsync(null, null))
            .ReturnsAsync(_standards);
        _apiMock
            .Setup(api => api.GetFunctionsAsync(It.IsAny<int>(), null))
            .ReturnsAsync(_zonesInProject);
        _apiMock
            .Setup(api => api.GetFunctionsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(_groupsInProject);
        _apiMock
            .Setup(api => api.GetFunctionsAsync(null, It.IsAny<int>()))
            .Throws(new HttpRequestException());

        _apiMock
            .Setup(api => api.GetFunctionByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new FunctionDto { Id = 1, Name = null, Description = null });
    }

    [Fact]
    public async Task GetFunctionsAsync_ShouldReturnFunctionZonesInProject_WhenNoZoneIdSpecified()
    {
        var apiFactoryMock = new Mock<IApiFactory<IFunctionApi>>();
        apiFactoryMock.Setup(x => x.Api).Returns(_apiMock.Object);

        var functionService = new FunctionService(apiFactoryMock.Object);

        var result = (await functionService.GetFunctionsAsync(1)).ToList();
        result.Count.Should().Be(_zonesInProject.Count());
        result.First().Id.Should().Be(_zonesInProject.First().Id);
    }

    [Fact]
    public async Task GetFunctionsAsync_ShouldReturnFunctionGroupsInProject_WhenThereIsZoneIdSpecified()
    {
        var apiFactoryMock = new Mock<IApiFactory<IFunctionApi>>();
        apiFactoryMock.Setup(x => x.Api).Returns(_apiMock.Object);

        var functionService = new FunctionService(apiFactoryMock.Object);

        var result = (await functionService.GetFunctionsAsync(1, 1)).ToList();
        result.Count.Should().Be(_groupsInProject.Count());
        result.First().Id.Should().Be(_groupsInProject.First().Id);
    }

    [Fact]
    public async Task GetStandardFunctionGroupssAsync_ShouldReturnStandardFunctionGroups()
    {
        var apiFactoryMock = new Mock<IApiFactory<IFunctionApi>>();
        apiFactoryMock.Setup(x => x.Api).Returns(_apiMock.Object);

        var functionService = new FunctionService(apiFactoryMock.Object);

        var result = (await functionService.GetStandardFunctionGroupsAsync()).ToList();
        result.Count.Should().Be(_standards.Count());
        result.First().Id.Should().Be(_standards.First().Id);
    }

    [Fact]
    public async Task GetFunctionsAsync_ShouldThroughNetworkNotValidException_WhenThereIsNetworkError()
    {
        var apiMock = new Mock<IFunctionApi>();
        apiMock
            .Setup(api => api.GetFunctionsAsync(null, null))
            .Throws(new HttpRequestException());

        var apiFactoryMock = new Mock<IApiFactory<IFunctionApi>>();
        apiFactoryMock.Setup(x => x.Api).Returns(apiMock.Object);

        var functionService = new FunctionService(apiFactoryMock.Object);

        var action = async () => await functionService.GetStandardFunctionGroupsAsync();
        await action.Should().ThrowAsync<NetworkNotValidException>();
    }


    [Fact]
    public async Task GetStandardFunctionGroupsAsync_ShouldThroughNetworkNotValidException_WhenThereIsNetworkError()
    {
        var apiMock = new Mock<IFunctionApi>();
        apiMock
            .Setup(api => api.GetFunctionsAsync(null, null))
            .Throws(new HttpRequestException());

        var apiFactoryMock = new Mock<IApiFactory<IFunctionApi>>();
        apiFactoryMock.Setup(x => x.Api).Returns(apiMock.Object);

        var functionService = new FunctionService(apiFactoryMock.Object);

        var action = async () => await functionService.GetStandardFunctionGroupsAsync();
        await action.Should().ThrowAsync<NetworkNotValidException>();
    }
}