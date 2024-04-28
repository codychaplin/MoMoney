namespace MoMoney.Core.Services.Interfaces;

public interface IRecordAudioService
{
    bool IsRecording { get; }
    void StartRecord();
    string StopRecord();
}