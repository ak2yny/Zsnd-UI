using System;

namespace Zsnd_UI.lib
{
    /// <summary>
    /// Enum classes for Zsnd values
    /// </summary>
    public static class ZsndProperties
    {
        [Flags]
        public enum SoundF : byte
        {
            None = 0,
            Unk1 = 1,
            Unk2 = 2,
            Unk3 = 4,
            Unk4 = 8,
            Unk5 = 16,
            Unk6 = 32,
            Unk7 = 64,
            Unk8 = 128
        }

        [Flags]
        public enum SampleF : ushort
        {
            None = 0,
            Loop = 1,
            Stereo = 2,
            Unknown1 = 4,
            Unknown2 = 8,
            Unknown3 = 16,
            AmbientEmbedded = 32,
            FourChannels = Stereo | AmbientEmbedded,
            Unknown4 = 64,
            Unknown5 = 128
        }

        public enum Platform
        {
            PC,
            XBOX,
            XENO,
            PS2,
            PS3,
            GCUB
        }

        public static readonly Platform[] Platforms = Enum.GetValues<Platform>();

        public static uint[] SampleRates { get; } =
        [
            8000, 11025, 16000, 22050, 32000, 41000, 44100, 48000, 96000, 192000
        ];
    }
}
