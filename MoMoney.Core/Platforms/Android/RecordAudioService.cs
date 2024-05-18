using Android.Media;
using Android.OS;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.Platforms.Android;

public class RecordAudioService : IRecordAudioService
{
    MediaRecorder mediaRecorder;
    public bool IsRecording { get; private set; } = false;
    string storagePath => $"{FileSystem.Current.CacheDirectory}/{Constants.AUDIO_FILE_NAME}";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Checking Build.VERSION.SdkInt")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422:Validate platform compatibility", Justification = "Checking Build.VERSION.SdkInt")]
    public void StartRecord()
    {
        if (mediaRecorder == null)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                mediaRecorder = new MediaRecorder(Platform.AppContext);
            else
                mediaRecorder = new MediaRecorder();
            mediaRecorder.Reset();
            mediaRecorder.SetAudioSource(AudioSource.Mic);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                mediaRecorder.SetOutputFormat(OutputFormat.Ogg);
                mediaRecorder.SetAudioEncoder(AudioEncoder.Opus);
            }
            else
            {
                mediaRecorder.SetOutputFormat(OutputFormat.ThreeGpp);
                mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
            }
            mediaRecorder.SetAudioEncodingBitRate(24000);
            mediaRecorder.SetAudioSamplingRate(16000);
            mediaRecorder.SetOutputFile(storagePath);
            mediaRecorder.Prepare();
            mediaRecorder.Start();
        }
        else
        {
            mediaRecorder.Resume();
        }
        IsRecording = true;
    }

    public string StopRecord()
    {
        if (mediaRecorder == null)
        {
            return string.Empty;
        }
        mediaRecorder.Resume();
        mediaRecorder.Stop();
        mediaRecorder = null;
        IsRecording = false;
        return storagePath;
    }
}