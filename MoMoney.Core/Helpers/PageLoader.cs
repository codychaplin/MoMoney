
namespace MoMoney.Core.Helpers;

public static class PageLoader
{
    public static async Task Load(Func<Task> targetMethod)
    {
        await Task.Delay(400);
        await targetMethod();
    }
}