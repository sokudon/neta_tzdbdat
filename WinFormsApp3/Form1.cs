using System;
using System.Drawing;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.Encodings;


namespace WinFormsApp3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        /*
         
    private static void load(DataInputStream dis) throws IOException {
        if (dis.readByte() != 1) {
            throw new StreamCorruptedException("File format not recognised");
        }
        // group
        String groupId = dis.readUTF();
        if ("TZDB".equals(groupId) == false) {
            throw new StreamCorruptedException("File format not recognised");
        }
        // versions, only keep the last one
        int versionCount = dis.readShort();
        for (int i = 0; i < versionCount; i++) {
            versionId = dis.readUTF();

        }
        // regions
        int regionCount = dis.readShort();
        String[] regionArray = new String[regionCount];
        for (int i = 0; i < regionCount; i++) {
            regionArray[i] = dis.readUTF();
        }
        // rules
        int ruleCount = dis.readShort();
        ruleArray = new byte[ruleCount][];
        for (int i = 0; i < ruleCount; i++) {
            byte[] bytes = new byte[dis.readShort()];
            dis.readFully(bytes);
            ruleArray[i] = bytes;
        }
        // link version-region-rules, only keep the last version, if more than one
        for (int i = 0; i < versionCount; i++) {
            regionCount = dis.readShort();
            regions = new String[regionCount];
            indices = new int[regionCount];
            for (int j = 0; j < regionCount; j++) {
                regions[j] = regionArray[dis.readShort()];
                indices[j] = dis.readShort();
            }
        }
        // remove the following ids from the map, they
        // are excluded from the "old" ZoneInfo
        zones.remove("ROC");
        for (int i = 0; i < versionCount; i++) {
            int aliasCount = dis.readShort();
            aliases.clear();
            for (int j = 0; j < aliasCount; j++) {
                String alias = regionArray[dis.readShort()];
                String region = regionArray[dis.readShort()];
                aliases.put(alias, region);
            }
        }
        // old us time-zone names
        aliases.putAll(ZoneId.SHORT_IDS);

           /////////////////////////Ser/////////////////////////////////
    public static ZoneInfo getZoneInfo(DataInput in, String zoneId) throws Exception {
        byte type = in.readByte();
        // TBD: assert ZRULES:
        int stdSize = in.readInt();
        long[] stdTrans = new long[stdSize];
        for (int i = 0; i < stdSize; i++) {
            stdTrans[i] = readEpochSec(in);
        }
        int [] stdOffsets = new int[stdSize + 1];
        for (int i = 0; i < stdOffsets.length; i++) {
            stdOffsets[i] = readOffset(in);
        }
        int savSize = in.readInt();
        long[] savTrans = new long[savSize];
        for (int i = 0; i < savSize; i++) {
            savTrans[i] = readEpochSec(in);
        }
        int[] savOffsets = new int[savSize + 1];
        for (int i = 0; i < savOffsets.length; i++) {
            savOffsets[i] = readOffset(in);
        }
        int ruleSize = in.readByte();
        ZoneOffsetTransitionRule[] rules = new ZoneOffsetTransitionRule[ruleSize];
        for (int i = 0; i < ruleSize; i++) {
            rules[i] = new ZoneOffsetTransitionRule(in);
        }
        return getZoneInfo(zoneId, stdTrans, stdOffsets, savTrans, savOffsets, rules);
    }

    public static int readOffset(DataInput in) throws IOException {
        int offsetByte = in.readByte();
        return offsetByte == 127 ? in.readInt() : offsetByte * 900;
    }

    static long readEpochSec(DataInput in) throws IOException {
        int hiByte = in.readByte() & 255;
        if (hiByte == 255) {
            return in.readLong();
        } else {
            int midByte = in.readByte() & 255;
            int loByte = in.readByte() & 255;
            long tot = ((hiByte << 16) + (midByte << 8) + loByte);
            return (tot * 900) - 4575744000L;
        }
    }// A simple/raw version of j.t.ZoneOffsetTransitionRule
    // timeEndOfDay is included in secondOfDay as "86,400" secs.
    private static class ZoneOffsetTransitionRule {
        private final int month;
        private final byte dom;
        private final int dow;
        private final int secondOfDay;
        private final int timeDefinition;
        private final int standardOffset;
        private final int offsetBefore;
        private final int offsetAfter;

        ZoneOffsetTransitionRule(DataInput in) throws IOException {
            int data = in.readInt();
            int dowByte = (data & (7 << 19)) >>> 19;
            int timeByte = (data & (31 << 14)) >>> 14;
            int stdByte = (data & (255 << 4)) >>> 4;
            int beforeByte = (data & (3 << 2)) >>> 2;
            int afterByte = (data & 3);

            this.month = data >>> 28;
            this.dom = (byte)(((data & (63 << 22)) >>> 22) - 32);
            this.dow = dowByte == 0 ? -1 : dowByte;
            this.secondOfDay = timeByte == 31 ? in.readInt() : timeByte * 3600;
            this.timeDefinition = (data & (3 << 12)) >>> 12;
            this.standardOffset = stdByte == 255 ? in.readInt() : (stdByte - 128) * 900;
            this.offsetBefore = beforeByte == 3 ? in.readInt() : standardOffset + beforeByte * 1800;
            this.offsetAfter = afterByte == 3 ? in.readInt() : standardOffset + afterByte * 1800;
        }

         */

        private static Dictionary<string, string> zones = new Dictionary<string, string>();
        private static Dictionary<string, string> aliases = new Dictionary<string, string>();
        private static string[] regionArray; // 事前に読み込まれたタイムゾーンID配列
        private static string[] regions; 
        private static int[] indices;
        private static byte[][] ruleArray;
        private static int[] ruleArray_offset;
         int target_index = -1;

        private void button1_Click(object sender, EventArgs e)
        {
            string tzst = comboBox1.Text;

            string filePath = "tzdb.dat"; // tzdb.datファイルのパスを指定してください
            byte[] bs = File.ReadAllBytes(filePath);
            // バイト配列をASCII文字列に変換
            byte[] name = new byte[40];

            // バイト配列を使用する処理をここに追加します
            int tzdb = bs[2];

            Array.ConstrainedCopy(bs, 3, name, 0, tzdb);
            string asciiString = Encoding.ASCII.GetString(name).TrimEnd('\0');
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(asciiString);


            int versionCount = bs[7] * 256 + bs[8];


            tzdb = bs[10];
            Array.ConstrainedCopy(bs, 11, name, 0, tzdb);
            asciiString = Encoding.ASCII.GetString(name).TrimEnd('\0');
            sb.AppendLine(asciiString);


            int maxcount = bs[16] * 256 + bs[17];
            int pointer = 18;
            int strlen = 0;

            sb.AppendLine("tzfiles:" + maxcount.ToString());
            regionArray = new string[maxcount];
            // // regions
            for (var i = 0; i < maxcount; i++)
            {
                strlen = bs[pointer] * 256 + bs[pointer + 1];

                Array.ConstrainedCopy(bs, pointer + 2, name, 0, strlen);
                pointer += strlen + 2;
                asciiString = Encoding.ASCII.GetString(name).TrimEnd('\0').Substring(0, strlen);
                sb.AppendLine($"{i},{asciiString}");
                regionArray[i] = asciiString;
            }
            try
            { // rules
                int ruleCount = bs[pointer] * 256 + bs[pointer + 1];
                pointer += 2;
                ruleArray = new byte[ruleCount][]; 
                
                sb.AppendLine($"ruleCount:{ruleCount}");

                ruleArray_offset = new int[ruleCount];
                for (int i = 0; i < ruleCount; i++)
                {
                    int length = bs[pointer] * 256 + bs[pointer + 1];
                    pointer += 2;

                    byte[] bytes = new byte[length];
                    Array.Copy(bs, pointer, bytes, 0, length);
                    pointer += length;
                    ruleArray[i] = bytes;
                    ruleArray_offset[i] =pointer - length;
                }

                // link version-region-rules, only keep the last version, if more than one
                for (int i = 0; i < versionCount; i++)
                {
                    int regionCount = bs[pointer] * 256 + bs[pointer + 1];
                    pointer += 2;

                    sb.AppendLine($"regionCount:{regionCount}");

                    regions = new string[regionCount];
                    indices = new int[regionCount];
                    for (int j = 0; j < regionCount; j++)
                    {
                        int regionIndex = bs[pointer] * 256 + bs[pointer + 1];
                        pointer += 2;
                        regions[j] = regionArray[regionIndex];

                        indices[j] = bs[pointer] * 256 + bs[pointer + 1];
                        pointer += 2;


                        sb.AppendLine($" regionIndex:{regionIndex}");
                        sb.AppendLine($" regions[{j}]:{regions[j]}");
                        sb.AppendLine($"indices[{j}]:{indices[j]},ruleArray_offset[{indices[j]}]:{ruleArray_offset[indices[j]]},0x{ruleArray_offset[indices[j]].ToString("X")}");

                        if (regions[j] == tzst)
                        {
                            target_index = ruleArray_offset[indices[j]];
                        }
                    }

                }

                // aliases
                for (int i = 0; i < versionCount; i++)
                {
                    int aliasCount = bs[pointer] * 256 + bs[pointer + 1];
                    pointer += 2;
                    sb.AppendLine($" aliasCount:{aliasCount}");

                    aliases.Clear();
                    for (int j = 0; j < aliasCount; j++)
                    {
                        int aliasIndex = bs[pointer] * 256 + bs[pointer + 1];
                        pointer += 2;
                        string alias = regionArray[aliasIndex];

                        int regionIndex = bs[pointer] * 256 + bs[pointer + 1];
                        pointer += 2;
                        string region = regionArray[regionIndex];

                        aliases[alias] = region;

                        sb.AppendLine($"aliasIndex:{aliasIndex},{alias}");
                        sb.AppendLine($"regionIndex:{regionIndex},{region}");
                    }
                }

            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.Message);
            }

            //PST ロサンゼルスの謎のタイムゾーン追加（）
            LoadAliases();

            textBox1.Text = sb.ToString(); // zonesとaliasesを文字列に変換

            StringBuilder aliasesSb = new StringBuilder();
            foreach (var kvp in aliases)
            {
                aliasesSb.AppendLine($"{kvp.Key} -> {kvp.Value}");
            }

            textBox2.Text = Environment.NewLine + "//aliases\r\n" + aliasesSb.ToString();
            button2_Click(sender, e);

        }
        public static void LoadAliases()
        {
            // 仮にエイリアスに事前データを追加
            aliases["GMT"] = "Zulu";

            // old US time-zone names
            foreach (var pair in ShortIds)
            {
                aliases[pair.Key] = pair.Value;
            }

            // 結果の確認
            Console.WriteLine($"Aliases count: {aliases.Count}");
            foreach (var kvp in aliases)
            {
                Console.WriteLine($"{kvp.Key} -> {kvp.Value}");
            }
        }

        public static readonly Dictionary<string, string> ShortIds = new Dictionary<string, string>
    {
        { "ACT", "Australia/Darwin" },
        { "AET", "Australia/Sydney" },
        { "AGT", "America/Argentina/Buenos_Aires" },
        { "ART", "Africa/Cairo" },
        { "AST", "America/Anchorage" },
        { "BET", "America/Sao_Paulo" },
        { "BST", "Asia/Dhaka" },
        { "CAT", "Africa/Harare" },
        { "CNT", "America/St_Johns" },
        { "CST", "America/Chicago" },
        { "CTT", "Asia/Shanghai" },
        { "EAT", "Africa/Addis_Ababa" },
        { "ECT", "Europe/Paris" },
        { "IET", "America/Indiana/Indianapolis" },
        { "IST", "Asia/Kolkata" },
        { "JST", "Asia/Tokyo" },
        { "MIT", "Pacific/Apia" },
        { "NET", "Asia/Yerevan" },
        { "NST", "Pacific/Auckland" },
        { "PLT", "Asia/Karachi" },
        { "PNT", "America/Phoenix" },
        { "PRT", "America/Puerto_Rico" },
        { "PST", "America/Los_Angeles" },
        { "SST", "Pacific/Guadalcanal" },
        { "VST", "Asia/Ho_Chi_Minh" },
        { "EST", "America/Panama" },
        { "MST", "America/Phoenix" },
        { "HST", "Pacific/Honolulu" }
    };


        public class ZoneInfoReader
        {
            public static ZoneInfo GetZoneInfo(byte[] bs, ref int pointer, string zoneId)
            {
                byte type = bs[pointer++]; // readByte
                                           // TBD: assert ZRULES:

                int stdSize = (bs[pointer] << 24) | (bs[pointer + 1] << 16) | (bs[pointer + 2] << 8) | bs[pointer + 3];
                pointer += 4; // readInt

                long[] stdTrans = new long[stdSize];
                for (int i = 0; i < stdSize; i++)
                {
                    stdTrans[i] = ReadEpochSec(bs, ref pointer);
                }

                int[] stdOffsets = new int[stdSize + 1];
                for (int i = 0; i < stdOffsets.Length; i++)
                {
                    stdOffsets[i] = ReadOffset(bs, ref pointer);
                }

                int savSize = (bs[pointer] << 24) | (bs[pointer + 1] << 16) | (bs[pointer + 2] << 8) | bs[pointer + 3];
                pointer += 4; // readInt

                long[] savTrans = new long[savSize];
                for (int i = 0; i < savSize; i++)
                {
                    savTrans[i] = ReadEpochSec(bs, ref pointer);
                }

                int[] savOffsets = new int[savSize + 1];
                for (int i = 0; i < savOffsets.Length; i++)
                {
                    savOffsets[i] = ReadOffset(bs, ref pointer);
                }

                int ruleSize = bs[pointer++]; // readByte
                ZoneOffsetTransitionRule[] rules = new ZoneOffsetTransitionRule[ruleSize];
                for (int i = 0; i < ruleSize; i++)
                {
                    rules[i] = new ZoneOffsetTransitionRule(bs, ref pointer); // 仮実装
                }

                return GetZoneInfo(zoneId, stdTrans, stdOffsets, savTrans, savOffsets, rules);
            }

            public static int ReadOffset(byte[] bs, ref int pointer)
            {
                int offsetByte = bs[pointer++]; // readByte
                return offsetByte == 127 ? BitConverter.ToInt32(bs, pointer += 4) - 4 : offsetByte * 900;
            }

            public static long ReadEpochSec(byte[] bs, ref int pointer)
            {
                int hiByte = bs[pointer++] & 255; // readByte
                if (hiByte == 255)
                {
                    long result = BitConverter.ToInt64(bs, pointer);
                    pointer += 8; // readLong
                    return result;
                }
                else
                {
                    int midByte = bs[pointer++] & 255;
                    int loByte = bs[pointer++] & 255;
                    long tot = ((long)hiByte << 16) + (midByte << 8) + loByte;
                    return (tot * 900) - 4575744000L;
                }
            }

            // 仮の ZoneInfo クラス
            public class ZoneInfo
            {
                public string ZoneId { get; }
                public long[] StdTrans { get; }
                public int[] StdOffsets { get; }
                public long[] SavTrans { get; }
                public int[] SavOffsets { get; }
                public ZoneOffsetTransitionRule[] Rules { get; }

                public ZoneInfo(string zoneId, long[] stdTrans, int[] stdOffsets, long[] savTrans, int[] savOffsets, ZoneOffsetTransitionRule[] rules)
                {
                    ZoneId = zoneId;
                    StdTrans = stdTrans;
                    StdOffsets = stdOffsets;
                    SavTrans = savTrans;
                    SavOffsets = savOffsets;
                    Rules = rules;
                }
            }

            // 仮の ZoneOffsetTransitionRule クラス
            public class ZoneOffsetTransitionRule
            {
                public ZoneOffsetTransitionRule(byte[] bs, ref int pointer)
                {
                    // 実際の実装が必要。仮にポインタを進めるだけ
                    pointer += 20; // 仮のサイズ
                }
            }

            // 仮の GetZoneInfo メソッド
            private static ZoneInfo GetZoneInfo(string zoneId, long[] stdTrans, int[] stdOffsets, long[] savTrans, int[] savOffsets, ZoneOffsetTransitionRule[] rules)
            {
                return new ZoneInfo(zoneId, stdTrans, stdOffsets, savTrans, savOffsets, rules);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string tzst = comboBox1.Text;

            byte[] bs = File.ReadAllBytes("tzdb.dat");
            int pointer = target_index;//0x9d29;
            if (pointer == -1)
            {
                textBox3.Text = "Not Found";
                return;
            }
            bool ch = checkBox1.Checked;

            var zoneInfo = ZoneInfoReader.GetZoneInfo(bs, ref pointer, tzst);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Zone: {zoneInfo.ZoneId}");

            sb.Append("StdTrans: ");
            foreach (var trans in zoneInfo.StdTrans)
            {
                if (ch)
                {
                    const long MinSeconds = -62135596800L;
                    const long MaxSeconds = 253402300799L;
                    if (trans >= MinSeconds && trans <= MaxSeconds)
                    {
                        DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(trans).UtcDateTime;
                        sb.Append(utcDateTime.ToString("u") + ",");
                    }
                    else
                    {
                        sb.Append($"OutOfRange({trans}),");
                    }
                }
                else
                {
                    sb.Append(trans.ToString() + ",");
                }
            }
            sb.AppendLine();

            sb.Append("StdOffsets: ");
            foreach (var offset in zoneInfo.StdOffsets)
            {
                if (ch)
                {   
                    var offset_d = offset /3600;
                    sb.Append(offset_d.ToString() + ",");
                }
                else { 
                sb.Append(offset.ToString() + ",");
                }
            }
            sb.AppendLine();

            sb.Append("SavTrans: ");
            foreach (var trans in zoneInfo.SavTrans)
            {
                if (ch)
                {
                    const long MinSeconds = -62135596800L;
                    const long MaxSeconds = 253402300799L;
                    if (trans >= MinSeconds && trans <= MaxSeconds)
                    {
                        DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(trans).UtcDateTime;
                        sb.Append(utcDateTime.ToString("u") + ",");
                    }
                    else
                    {
                        sb.Append($"OutOfRange({trans}),");
                    }
                }
                else
                {
                    sb.Append(trans.ToString() + ",");
                }
            }
            sb.AppendLine();

            sb.Append("SavOffsets: ");
            foreach (var offset in zoneInfo.SavOffsets)
            {
                if (ch)
                {
                    var offset_d = offset / 3600;
                    sb.Append(offset_d.ToString() + ",");
                }
                else
                {
                    sb.Append(offset.ToString() + ",");
                }
            }
            sb.AppendLine();

            textBox3.Text = sb.ToString();
        }

    }
    
}
