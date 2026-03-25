using System;
using static Zsnd_UI.lib.ZsndProperties;

namespace Zsnd_UI.lib
{
    public class ZsndPlatform
    {
        private static readonly uint[] PlatIDbe = [0x50432020, 0x58424F58, 0x58454E4F, 0x50533220, 0x50533320, 0x47435542];
        private static readonly uint[] PlatIDle = [0x20204350, 0x584F4258, 0x4F4E4558, 0x20325350, 0x20335350, 0x42554347];
        /// <summary>
        /// The saved <see cref="Platform"/>. If invalid, defaults to <see cref="Platform.PC"/>.
        /// </summary>
        public Platform ID { get; set; } = 0;
        /// <summary>
        /// Whether the saved <see cref="ID"/> is <see cref="Platform.PC"/>, <see cref="Platform.XBOX"/> or <see cref="Platform.XENO"/>.
        /// </summary>
        public bool IsMicrosoft { get; private set; } = true;
        /// <summary>
        /// Whether the saved <see cref="ID"/> is <see cref="Platform.PS2"/> or <see cref="Platform.PS3"/>.
        /// </summary>
        public bool IsPS { get; private set; }
        /// <summary>
        /// Whether the saved <see cref="ID"/> is <see cref="Platform.XENO"/>, <see cref="Platform.PS3"/> or Wii (<see cref="Platform.GCUB"/>, possibly includes Gamecube as well). These are all Big Endian.
        /// </summary>
        public bool Is7thGen { get; private set; }
        /// <summary>
        /// Whether the platform uses a headerless format (<see cref="ID"/> is <see cref="Platform.PC"/> or <see cref="Platform.XBOX"/> or <see cref="IsPS"/>).
        /// </summary>
        public bool IsHeaderless { get; private set; }
        /// <summary>
        /// The <see cref="ID"/> (padded with white space) as machine endian dependent <see cref="uint"/> representation.
        /// </summary>
        public uint Magic => BitConverter.IsLittleEndian ? PlatIDle[(int)ID] : PlatIDbe[(int)ID];
        /// <summary>
        /// The Size <see cref="uint"/>, depending on the saved <see cref="ID"/>.
        /// </summary>
        public uint SampleInfoSz => ID == Platform.XBOX ? 28u : ID == Platform.XENO ? 36u : IsPS ? 16u : 24u;
        /// <summary>
        /// The Size <see cref="uint"/>, depending on the saved <see cref="ID"/>.
        /// </summary>
        public uint FileInfoSz => ID == Platform.PC ? 76u : IsMicrosoft ? 84u : IsPS ? 8u : 12u;
        /// <summary>
        /// Whether the <see cref="String"/> is a valid <see cref="Platform"/>.
        /// </summary>
        public bool IsValid { get; private set; }
        /// <summary>
        /// The string representation of the saved <see cref="ID"/>. Is set, even if not a valid <see cref="Platform"/>.
        /// </summary>
        public string String { get; private set; }
        /// <summary>
        /// Save <paramref name="platform"/> to the <see cref="ID"/> <see cref="Platform"/> <see cref="System.Enum"/>.
        /// </summary>
        public ZsndPlatform(string platform)
        {
            if (Enum.TryParse(platform, out Platform P)) { Update(P); }
            ID = P; String = platform;
        }
        /// <summary>
        /// Save <paramref name="platform"/> to the <see cref="ID"/> <see cref="Platform"/> <see cref="System.Enum"/>.
        /// </summary>
        public ZsndPlatform(uint platform)
        {
            int i = Array.IndexOf(BitConverter.IsLittleEndian ? PlatIDle : PlatIDbe, platform);
            if (i == -1) { ID = 0; String = "PC"; return; }
            ID = (Platform)i;
            String = ID.ToString();
            Update(ID);
        }
        /// <summary>
        /// Update the other properties according to the <see cref="ID"/> <see cref="Platform"/> <see cref="System.Enum"/>.
        /// </summary>
        //public void Update() { Update(ID); String = ID.ToString(); }

        private void Update(Platform P)
        {
            IsMicrosoft = (int)P < 3;
            IsPS = P is Platform.PS2 or Platform.PS3;
            Is7thGen = P is Platform.XENO or Platform.PS3 or Platform.GCUB;
            IsHeaderless = !Is7thGen || P is Platform.PS3;
            IsValid = true;
        }
    }
}
