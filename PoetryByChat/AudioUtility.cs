using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Net;
using System.Web;

public static class AudioUtility
{
    public static void ReadText(string text)
    {

        Console.WriteLine($"TTS: {text}");
        PlayMP3FromURL($"https://scapi-eu.readspeaker.com/a/speak?key={Config.ReadSpeakerAPIKey}&lang={Config.ReadSpeakerLanguageCode}&voice={Config.ReadSpeakerVoice}&text={HttpUtility.UrlEncode(text)}");
    }

    private static void PlayMP3FromURL(string url, int timeout = 100000)
    {
        var playThread  = new Thread(timeout =>
        {
            using (Stream ms = new MemoryStream())
            {
                var response = WebRequest.Create(url)
                    .GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    byte[] buffer = new byte[32768];
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        Debug.WriteLine(string.Join(" ", buffer));
                        ms.Write(buffer, 0, read);
                    }
                }
                ms.Position = 0;
                using (WaveStream blockAlignedStream =
                    new BlockAlignReductionStream(
                        WaveFormatConversionStream.CreatePcmStream(
                            new Mp3FileReader(ms))))
                {
                    using (var waveOut = new DirectSoundOut())
                    {
                        waveOut.Init(blockAlignedStream);
                        waveOut.PlaybackStopped += (sender, e) =>
                        {
                            waveOut.Stop();
                        };
                        waveOut.Play();
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                }
            }
        }, timeout);

        playThread.IsBackground = true;
        playThread.Start(timeout);
    }
}