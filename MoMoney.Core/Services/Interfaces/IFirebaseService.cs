
namespace MoMoney.Core.Services.Interfaces;

public interface IFirebaseService
{
    void LogFirebaseEvent(string eventName, IDictionary<string, string> parameters);
}