using System;
using System.Linq;
using AE.PID.Visio.Core;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using Splat;

namespace AE.PID.ViewModels;

public class InitialSetupPageViewModel(IConfigurationService? configuration = null)
    : ViewModelBase, IValidatableViewModel
{
    #region Resolution

    private readonly IConfigurationService _configuration =
        configuration ?? Locator.Current.GetService<IConfigurationService>()!;

    #endregion

    private string _server = string.Empty;
    private string _user = string.Empty;

    #region Read-Only Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();

    #endregion

    public IValidationContext ValidationContext { get; } = new ValidationContext();

    private void SaveChanges()
    {
        if (_configuration.Server != _server)
            _configuration.Server = _server;

        if (_configuration.UserId != _user)
            _configuration.UserId = _user;
    }

    private static bool IsValidHttpUrl(string? url)
    {
        if (url is null) return false;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        return false;
    }

    private static bool IsAllDigits(string? input)
    {
        return input?.All(char.IsDigit) ?? false;
    }

    #region Setup

    protected override void SetupCommands()
    {
        OkCancelFeedbackViewModel.Ok = ReactiveCommand.CreateRunInBackground(SaveChanges);
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }

    protected override void SetupStart()
    {
        _server = _configuration.Server;
        _user = _configuration.UserId;

        this.ValidationRule(
            viewModel => viewModel.Server,
            IsValidHttpUrl,
            "输入的 HTTP 地址格式不正确，请检查并重试。");

        this.ValidationRule(
            viewModel => viewModel.User,
            IsAllDigits,
            "请输入工号。");
    }

    #endregion

    #region Read-Write Properties

    public string Server
    {
        get => _server;
        set => this.RaiseAndSetIfChanged(ref _server, value);
    }

    public string User
    {
        get => _user;
        set => this.RaiseAndSetIfChanged(ref _user, value);
    }

    #endregion
}