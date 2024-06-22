using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public abstract partial class BaseEditViewModel<TService, TLogger> : ObservableObject, IQueryAttributable where TService : class where TLogger : class
{
    protected readonly TService service;
    protected readonly ILoggerService<TLogger> logger;

    [ObservableProperty] bool isEditMode = false;

    public BaseEditViewModel(TService _service, ILoggerService<TLogger> _logger)
    {
        service = _service;
        logger = _logger;
    }

    public abstract void ApplyQueryAttributes(IDictionary<string, object> query);

    protected abstract Task Add();

    protected abstract Task Edit();

    protected abstract Task Remove();
}