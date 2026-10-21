using MoMoney.Core.ViewModels.Tabs;

namespace MoMoney.Views.Tabs;

public partial class AddTransactionPage : ContentView
{
    AddTransactionViewModel? vm;

    public AddTransactionPage()
    {
        InitializeComponent();

        HandlerChanged += async (s, e) =>
        {
            vm = Handler?.MauiContext?.Services.GetService<AddTransactionViewModel>();
            if (vm == null)
                return;
            BindingContext = vm;

            await vm.Init();
        };
    }

    void OnTransactionTypeChanged(object? sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
    {
        if (vm == null) return;
        switch (e.NewIndex)
        {
            case 0:
                vm.GetIncomeCategoryCommand.Execute(null);
                break;
            case 1:
                vm.GetExpenseCategoriesCommand.Execute(null);
                break;
            case 2:
                vm.GetTransferCategoryCommand.Execute(null);
                break;
        }
    }

    private void btnClear_Clicked(object? sender, EventArgs e)
    {
        vm?.Clear();
        segTransactionType.SelectedIndex = -1;
    }

    private void btnEnter_Clicked(object? sender, EventArgs e)
    {
        vm?.ClearAfterAdd();
    }
}
