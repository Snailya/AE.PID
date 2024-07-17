using System;
using System.Linq;
using AE.PID.Services;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;

namespace AE.PID.ViewModels;

public class InitialSetupPageViewModel(ConfigurationService configuration) : ViewModelBase, IValidatableViewModel
{
    private string _server = configuration.Server;
    private string _user = configuration.UserId;

    #region Read-Only Properties

    public OkCancelFeedbackViewModel OkCancelFeedbackViewModel { get; } = new();

    #endregion

    public IValidationContext ValidationContext { get; } = new ValidationContext();

    private void SaveChanges()
    {
        if (configuration.Server != _server)
            configuration.Server = _server;

        if (configuration.UserId != _user)
            configuration.UserId = _user;
    }

    private bool IsValidHttpUrl(string? url)
    {
        if (url is null) return false;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        return false;
    }

    private bool IsAllDigits(string? input)
    {
        return input?.All(char.IsDigit) ?? false;
    }

    #region Setup

    protected override void SetupCommands()
    {
        OkCancelFeedbackViewModel.Ok = ReactiveCommand.Create(SaveChanges);
        OkCancelFeedbackViewModel.Cancel = ReactiveCommand.Create(() => { });
    }

    protected override void SetupStart()
    {
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