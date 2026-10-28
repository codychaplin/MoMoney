import { IOSOutputFormat, AudioQuality, requestRecordingPermissionsAsync, setAudioModeAsync, useAudioRecorder, type RecordingOptions } from 'expo-audio';
import { useCallback, useEffect, useRef, useState } from 'react';

/** Speech only: mono 16 kHz AAC keeps uploads small and Whisper accepts m4a on both platforms. */
const RECORDING_OPTIONS: RecordingOptions = {
  extension: '.m4a',
  sampleRate: 16000,
  numberOfChannels: 1,
  bitRate: 32000,
  android: { outputFormat: 'mpeg4', audioEncoder: 'aac' },
  ios: { outputFormat: IOSOutputFormat.MPEG4AAC, audioQuality: AudioQuality.MEDIUM, linearPCMBitDepth: 16, linearPCMIsBigEndian: false, linearPCMIsFloat: false },
  web: { mimeType: 'audio/webm', bitsPerSecond: 32000 },
};

/** Mic recorder for dictation. `start` resolves false when the permission is denied. */
export function useDictationRecorder() {
  const recorder = useAudioRecorder(RECORDING_OPTIONS);
  const [recording, setRecording] = useState(false);
  const active = useRef(false);

  const start = useCallback(async (): Promise<boolean> => {
    const { granted } = await requestRecordingPermissionsAsync();
    if (!granted) return false;
    await setAudioModeAsync({ allowsRecording: true, playsInSilentMode: true });
    await recorder.prepareToRecordAsync();
    recorder.record();
    active.current = true;
    setRecording(true);
    return true;
  }, [recorder]);

  /** Stops and returns the file uri, or null when nothing was recorded. */
  const stop = useCallback(async (): Promise<string | null> => {
    if (!active.current) return null;
    active.current = false;
    setRecording(false);
    try {
      await recorder.stop();
      return recorder.uri;
    } finally {
      await setAudioModeAsync({ allowsRecording: false }).catch(() => {});
    }
  }, [recorder]);

  useEffect(
    () => () => {
      if (active.current) void recorder.stop().catch(() => {});
    },
    [recorder],
  );

  return { recording, start, stop };
}
