using System;
using System.Drawing;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.Encodings;
using System.Buffers.Binary;
using System.Web;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using static WinFormsApp3.Form1.ZoneInfoReader;

namespace WinFormsApp3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }



        private static Dictionary<string, string> zones = new Dictionary<string, string>();
        private static Dictionary<string, string> aliases = new Dictionary<string, string>();
        private static string[] regionArray; // 事前に読み込まれたタイムゾーンID配列
        private static string[] regions;
        private static int[] indices;
        private static byte[][] ruleArray;
        private static int[] ruleArray_offset;
        int target_index = -1;

        private void time(long[] SavTrans, int[] SavOffsets)
        {
            try
            {
                DateTime utc = DateTime.UtcNow;
                string tzst = comboBox1.Text;
                string tmp = "";

                bool ch = div3600.Checked;
                bool sun = sunfix.Checked;

                // UTC時間をUnixタイムスタンプ（秒）に変換
                long unixTimestamp = ((DateTimeOffset)utc).ToUnixTimeSeconds();

                int svlen = SavTrans.Length;
                if (svlen == 0)
                {
                    double offsetSeconds = Convert.ToDouble(SavOffsets[0]);
                    int offsetSeconds_i = SavOffsets[0];
                    if (offsetSeconds_i > sun_big_offset * 3600)
                    {
                        offsetSeconds_i = offsetSeconds_i - sun_negative_sifhter * 3600;
                        offsetSeconds = offsetSeconds - sun_negative_sifhter * 3600;
                    }
                    var offset_st = (offsetSeconds_i / 3600).ToString("D2") + ":" + ((offsetSeconds_i % 3600) / 60).ToString("D2");

                    DateTimeOffset localTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).AddSeconds(offsetSeconds);
                    tmp = localTime.ToString("yyyy-MM-ddTHH:mm:ss+" + offset_st).Replace("+-", "-");
                    tmp = tmp + $"\r\nindex:0\r\ntradition:null\r\noffset0:{SavOffsets[0]}";

                    label1.Text = tmp;
                    return;
                }



                // バイナリーサーチを実行
                long max = SavTrans[svlen - 1];
                long min = SavTrans[0];
                int index = Array.BinarySearch(SavTrans, unixTimestamp);
                if (unixTimestamp > max)
                {
                    index = svlen - 1;
                }
                else if (unixTimestamp < min)
                {
                    index = -1;
                }
                else if (index < 0)
                {
                    index = -(index+1)-1; 
                }
                if(index < 0)
                {
                    index = -1;
                }

                if (index >= 0 && index < SavOffsets.Length)
                {
                    double offsetSeconds = Convert.ToDouble(SavOffsets[index + 1]);
                    int offsetSeconds_i = SavOffsets[index + 1];
                    if (offsetSeconds > sun_big_offset * 3600)
                    {
                        offsetSeconds = offsetSeconds - sun_negative_sifhter * 3600;
                        offsetSeconds_i = offsetSeconds_i - sun_negative_sifhter * 3600;
                    }
                    var offset_st = (offsetSeconds_i / 3600).ToString("D2") + ":" + ((offsetSeconds_i % 3600) / 60).ToString("D2");

                    DateTimeOffset localTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).AddSeconds(offsetSeconds);
                    tmp = localTime.ToString("yyyy-MM-ddTHH:mm:ss+" + offset_st).Replace("+-","-");
                }
                else
                {
                    tmp = "not found";
                    label1.Text = tmp;
                    return;
                }
                var trans = SavTrans[index];
                var tt = "";
                if (ch)
                {
                    const long MinSeconds = -62135596800L;
                    const long MaxSeconds = 253402300799L;
                    if (trans >= MinSeconds && trans <= MaxSeconds)
                    {

                        DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(trans).UtcDateTime;
                        tt = utcDateTime.ToString("u");
                    }
                    else
                    {
                        tt = "OutOfRange{trans},";
                    }
                }
                double offsetSeconds_ii = SavOffsets[index + 1];
                if (sun)
                {
                    if (offsetSeconds_ii > sun_big_offset * 3600)
                    {
                        offsetSeconds_ii = offsetSeconds_ii - sun_negative_sifhter * 3600;
                    }
                    offsetSeconds_ii = offsetSeconds_ii / 3600;
                }



                tmp = tmp + $"\r\nindex+1:{index + 1}\r\ntradition:{tt}\r\noffset+1:{offsetSeconds_ii}";

                label1.Text = tmp;
            }
            catch (Exception ex)
            {
                label1.Text = ex.Message;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string tzst = comboBox1.Text;


            foreach (var pair in ShortIds)
            {
                if (tzst == pair.Key)
                {
                    tzst = pair.Value;
                }
            }

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
                    ruleArray_offset[i] = pointer - length;
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
                    rules[i] = new ZoneOffsetTransitionRule(bs, ref pointer);
                }

                return GetZoneInfo(zoneId, stdTrans, stdOffsets, savTrans, savOffsets, rules);
            }

            public static int ReadOffset(byte[] bs, ref int pointer)
            {
                int offsetByte = bs[pointer]; // readByte
                pointer++;
                int pp = Convert.ToInt32(pointer);
                string log2 = pp.ToString("X8");
                string logw = offsetByte.ToString("X");

                string ab = log2 + logw;
                string aab = log2 + logw;

                if (offsetByte == 127)
                {
                    byte[] b = new byte[4];
                    Array.Copy(bs, pointer, b, 0, 4);
                    Array.Reverse(b);
                    pointer += 4; // 4バイト読み込んだのでポインタを進める
                    return BitConverter.ToInt32(b, 0);
                }
                else
                {
                    return offsetByte * 900;
                }
            }

            public static long ReadEpochSec(byte[] bs, ref int pointer)
            {
                int hiByte = bs[pointer++] & 255; // readByte
                if (hiByte == 255)
                {
                    long result = BinaryPrimitives.ReadInt64BigEndian(bs.AsSpan(pointer));
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


            public class ZoneOffsetTransitionRule
            {
                private readonly int month;
                private readonly byte dom;
                private readonly int dow;
                private readonly int secondOfDay;
                private readonly int timeDefinition;
                private readonly int standardOffset;
                private readonly int offsetBefore;
                private readonly int offsetAfter;

                public ZoneOffsetTransitionRule(byte[] bs, ref int pointer)
                {
                    int data = BinaryPrimitives.ReadInt32BigEndian(bs.AsSpan(pointer));
                    pointer += 4;

                    int dowByte = (data & (7 << 19)) >>> 19;
                    int timeByte = (data & (31 << 14)) >>> 14;
                    int stdByte = (data & (255 << 4)) >>> 4;
                    int beforeByte = (data & (3 << 2)) >>> 2;
                    int afterByte = (data & 3);

                    this.month = data >>> 28;
                    this.dom = (byte)(((data & (63 << 22)) >>> 22) - 32);
                    this.dow = dowByte == 0 ? -1 : dowByte;
                    this.secondOfDay = timeByte == 31 ? BinaryPrimitives.ReadInt32BigEndian(bs.AsSpan(pointer)) : timeByte * 3600;
                    if (timeByte == 31) pointer += 4;
                    this.timeDefinition = (data & (3 << 12)) >>> 12;
                    this.standardOffset = stdByte == 255 ? BinaryPrimitives.ReadInt32BigEndian(bs.AsSpan(pointer)) : (stdByte - 128) * 900;
                    if (stdByte == 255) pointer += 4;
                    this.offsetBefore = beforeByte == 3 ? BinaryPrimitives.ReadInt32BigEndian(bs.AsSpan(pointer)) : standardOffset + beforeByte * 1800;
                    if (beforeByte == 3) pointer += 4;
                    this.offsetAfter = afterByte == 3 ? BinaryPrimitives.ReadInt32BigEndian(bs.AsSpan(pointer)) : standardOffset + afterByte * 1800;
                    if (afterByte == 3) pointer += 4;
                }

                // 必要に応じてプロパティを追加
                public int Month => month;
                public byte DayOfMonth => dom;
                public int DayOfWeek => dow;
                public int SecondOfDay => secondOfDay;
                public int TimeDefinition => timeDefinition;
                public int StandardOffset => standardOffset;
                public int OffsetBefore => offsetBefore;
                public int OffsetAfter => offsetAfter;
            }

            private static ZoneInfo GetZoneInfo(string zoneId, long[] stdTrans, int[] stdOffsets, long[] savTrans, int[] savOffsets, ZoneOffsetTransitionRule[] rules)
            {
                return new ZoneInfo(zoneId, stdTrans, stdOffsets, savTrans, savOffsets, rules);
            }
        }

        //+40以上のオフセットを持つタイムゾーンのための補正、-64時間
        //マイナス回避のためだと思われる 2^6 =64 だからシフトかなんか
        const int sun_negative_sifhter = 64;
        const int sun_big_offset = 40;

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
            bool ch = div3600.Checked;
            bool sun = sunfix.Checked;

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
                double offset_d = offset;
                if (sun & offset_d > sun_big_offset * 3600)
                {
                    offset_d = offset_d - sun_negative_sifhter * 3600;
                }
                if (ch)
                {
                    offset_d = offset_d / 3600;

                    sb.Append(offset_d.ToString() + ",");
                }
                else
                {
                    sb.Append(offset_d.ToString() + ",");
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

                double offset_d = offset;
                if (sun & offset_d > sun_big_offset * 3600)
                {
                    offset_d = offset_d - sun_negative_sifhter * 3600;
                }
                if (ch)
                {
                    offset_d = offset_d / 3600;

                    sb.Append(offset_d.ToString() + ",");
                }
                else
                {
                    sb.Append(offset_d.ToString() + ",");
                }
            }

            sb.AppendLine();
            sb.AppendLine("ZoneInfoRules: ");
            foreach (var rr in zoneInfo.Rules)
            {
                if (ch)
                {
                    var tradision_moment = (rr.SecondOfDay / 3600).ToString("D2") + ":" + ((rr.SecondOfDay % 3600)/60).ToString("D2");
                    double offset_d = rr.StandardOffset / 3600;
                    double offset_b = rr.OffsetBefore / 3600;
                    double offset_a = rr.OffsetAfter / 3600;
                    sb.AppendLine($"Month{rr.Month},DayOfMonth:{rr.DayOfMonth},DayOfWeek:{rr.DayOfWeek}" +
                    $", SecondOfDay:{tradision_moment},TimeDefinition: {rr.TimeDefinition}," +
                    $"StandardOffset: {offset_d},OffsetBefore: {offset_b},OffsetAfter: {offset_a}");
                }
                else
                {
                    sb.AppendLine($"Month{rr.Month},DayOfMonth:{rr.DayOfMonth},DayOfWeek:{rr.DayOfWeek}" +
                    $", SecondOfDay:{rr.SecondOfDay},TimeDefinition: {rr.TimeDefinition}," +
                    $"StandardOffset: {rr.StandardOffset},OffsetBefore: {rr.OffsetBefore},OffsetAfter: {rr.OffsetAfter}");
                }
            }
            sb.AppendLine();

            textBox3.Text = sb.ToString();

            time(zoneInfo.SavTrans, zoneInfo.SavOffsets);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }

}
