using NAudio.Wave;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NK2Tray
{
    class MediaTools
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);

        public const int VK_MEDIA_NEXT_TRACK = 0xB0;
        public const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        public const int VK_MEDIA_PREV_TRACK = 0xB1;
        public const int VK_MEDIA_STOP = 0xB2;
       /* public const int VK_NUMPAD_9 = 0x69;
        public const int VK_NUMPAD_8 = 0x68;
        public const int VK_NUMPAD_7 = 0x67;
        public const int VK_NUMPAD_6 = 0x66;
        public const int VK_NUMPAD_5 = 0x65;
        public const int VK_NUMPAD_4 = 0x64;
        public const int VK_NUMPAD_3 = 0x63;
        public const int VK_NUMPAD_2 = 0x62;
        public const int VK_NUMPAD_1 = 0x61;
        public const int VK_NUMPAD_0 = 0x60;
        public const int VK_CONTROL = 0x14;
        public const int VK_ALT = 0x11;*/

        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag

        public static void Play()
        {
            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }

        public static void Stop()
        {
             keybd_event(VK_MEDIA_STOP, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
             keybd_event(VK_MEDIA_STOP, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
          
        }

        public static void Next()
        {
            keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
            keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }

        public static void Previous()
        {
            keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
            keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }
        public static void button1(int select)
        {
            string source;
            if (select == 1)
                source = @"..\..\..\NK2Tray\Sounds\bruh.wav";
            else if(select == 2)
                source = @"..\..\..\NK2Tray\Sounds\FBI.wav";
            else if(select == 3)
                source = @"..\..\..\NK2Tray\Sounds\onetap.wav";
            else if(select == 4)
                source = @"..\..\..\NK2Tray\Sounds\Snusk.wav";
            else if (select == 5)
                source = @"..\..\..\NK2Tray\Sounds\barn.wav";
            else if (select == 6)
                source = @"..\..\..\NK2Tray\Sounds\Unstop.wav";
            else if (select == 7)
                source = @"..\..\..\NK2Tray\Sounds\ens.wav";
            else
                source = @"..\..\..\NK2Tray\Sounds\Fuskaj.wav";

            play(source);
        }

        public static void play(string path)
        {
            WaveFileReader wav = new WaveFileReader(path);
            WaveFileReader wav2 = new WaveFileReader(path);
            var output = new WaveOutEvent { DeviceNumber = 0 };
            var output2 = new WaveOutEvent { DeviceNumber = 1 };

            output.Init(wav);
            output2.Init(wav2);
            output.Play();
            output2.Play();
            //wav.Dispose();
           // output2.Dispose();

        }

    }
}
