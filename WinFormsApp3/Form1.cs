using System.Buffers.Binary;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
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

        private string time(long[] SavTrans, int[] SavOffsets, int rules_length)
        {
            try
            {
                DateTime date;
                string dd = Properties.Settings.Default.testdt;
                // UTC時間をUnixタイムスタンプ（秒）に変換
                long unixTimestamp = 0;
                if (!DateTime.TryParse(dd, out date))
                {
                    unixTimestamp = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

                }
                else
                {
                    unixTimestamp = ((DateTimeOffset)date).ToUnixTimeSeconds();
                }

                string tzst = tzdb_names.Text;
                string tmp = "";

                bool ch = div3600.Checked;


                int svlen = SavTrans.Length;
                if (svlen < 2)
                {
                    int set_index = 0;
                    if (svlen == 1)
                    {
                        set_index = 1;
                    }
                    double offsetSeconds = Convert.ToDouble(SavOffsets[set_index]);
                    int offsetSeconds_i = SavOffsets[set_index];
                    var offset_st = (offsetSeconds_i / 3600).ToString("D2") + ":" + ((offsetSeconds_i % 3600) / 60).ToString("D2");

                    DateTimeOffset localTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).AddSeconds(offsetSeconds);
                    tmp = localTime.ToString("yyyy-MM-ddTHH:mm:ss+" + offset_st).Replace("+-", "-");
                    tmp = tmp + $"\r\nindex:0\r\ntradition:null\r\noffset0:{SavOffsets[set_index]}";

                    return tmp;

                }


                // バイナリーサーチを実行
                long max = SavTrans[svlen - 1];
                long min = SavTrans[0];
                int index = Array.BinarySearch(SavTrans, unixTimestamp);
                if (unixTimestamp > max)
                {
                    index = svlen - 1;
                    if (ap_rules.Checked && rules_length > 1)
                    {
                        return "MAX";
                    }
                }
                else if (unixTimestamp < min)
                {
                    index = -1;
                }
                else if (index < 0)
                {
                    index = -(index + 1) - 1;
                }
                if (index < 0)
                {
                    index = -1;
                }

                if (index >= -1 && index < SavOffsets.Length)
                {
                    double offsetSeconds = Convert.ToDouble(SavOffsets[index + 1]);
                    int offsetSeconds_i = SavOffsets[index + 1];
                    var offset_st = (offsetSeconds_i / 3600).ToString("D2") + ":" + ((offsetSeconds_i % 3600) / 60).ToString("D2");

                    DateTimeOffset localTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).AddSeconds(offsetSeconds);
                    tmp = localTime.ToString("yyyy-MM-ddTHH:mm:ss+" + offset_st).Replace("+-", "-");
                }

                const long MinSeconds = -62135596800L;
                const long MaxSeconds = 253402300799L;
                long trans = 0;
                if (index >= 0) { trans = SavTrans[index]; }
                else { trans = MinSeconds; }

                var tt = "";
                if (ch)
                {
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
                offsetSeconds_ii = offsetSeconds_ii / 3600;



                tmp = tmp + $"\r\nindex+1:{index + 1}\r\ntradition:{tt}\r\noffset+1:{offsetSeconds_ii}";

                return tmp;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string tzst = tzdb_names.Text;
            Properties.Settings.Default.tzst = tzst;


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
                int offsetByte = (sbyte)bs[pointer++]; // readByte
                //pointer++;


                if (offsetByte == 127)
                {
                    int offsetvalue = BinaryPrimitives.ReadInt32BigEndian(bs.AsSpan(pointer));
                    pointer += 4;
                    return offsetvalue;
                }
                else
                {
                    return offsetByte * 900;
                }
            }

            const long sun_epoc_shifter = 4575744000L;//- 1824年12月27日　+ 2115/01/01 09:00 152年　52960日　


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
                    return (tot * 900) - sun_epoc_shifter;
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



            private static ZoneInfo GetZoneInfo(string zoneId, long[] stdTrans, int[] stdOffsets, long[] savTrans, int[] savOffsets, ZoneOffsetTransitionRule[] rules)
            {
                return new ZoneInfo(zoneId, stdTrans, stdOffsets, savTrans, savOffsets, rules);
            }

        }


        private void button2_Click(object sender, EventArgs e)
        {
            string tzst = tzdb_names.Text;

            byte[] bs = File.ReadAllBytes("tzdb.dat");
            int pointer = target_index;//0x9d29;
            if (pointer == -1)
            {
                textBox3.Text = "Not Found";
                return;
            }
            bool ch = div3600.Checked;


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

            string[] td_name= { "UTC", "Wall", "Standard" };
            string td_tmo = "UTC";
            string days = "Sun";
            int td_idx = 0;
            foreach (var rr in zoneInfo.Rules)
            {
                if (ch)
                {
                    var tradision_moment = (rr.SecondOfDay / 3600).ToString("D2") + ":" + ((rr.SecondOfDay % 3600) / 60).ToString("D2");
                    string offset_d = ("+" + tohhmm(rr.StandardOffset, 0)).Replace("+-", "-");
                    string offset_b = ("+" + tohhmm(rr.OffsetBefore, 0)).Replace("+-", "-");
                    string offset_a = ("+" + tohhmm(rr.OffsetAfter, 0)).Replace("+-", "-");

                    td_idx = rr.TimeDefinition;
                    td_tmo = td_name[td_idx];
                    DayOfWeek day = (DayOfWeek)(int)(rr.DayOfWeek%7);
                    string dayName = day.ToString(); // "Wednesday"


                    sb.AppendLine($"{offset_b}=>{offset_a}, {dayName} on or after {rr.Month}/{rr.DayOfMonth} " +
                    $"at {tradision_moment} {td_tmo}," +
                    $"StandardOffset: {offset_d}");
                }
                else
                {
                    td_idx = rr.TimeDefinition;
                    td_tmo = td_name[td_idx];
                    sb.AppendLine($"Month{rr.Month},DayOfMonth:{rr.DayOfMonth},DayOfWeek:{rr.DayOfWeek}" +
                    $", SecondOfDay:{rr.SecondOfDay},TimeDefinition: {td_idx}({td_tmo}),\"" +
                    $"StandardOffset: {rr.StandardOffset},OffsetBefore: {rr.OffsetBefore},OffsetAfter: {rr.OffsetAfter}");
                }
            }

            //https://grok.com/share/bGVnYWN5_8278e333-2177-4e8e-abe5-9a6a2dd66e12
            string fake_posixs = "";
            if (zoneInfo.Rules.Length == 0)
            {

            }
            else
            {
                var stdOffset = zoneInfo.Rules[0].StandardOffset;
                var ruleList = zoneInfo.Rules.ToList();
                var dstStartRule = ruleList.First(r => r.OffsetAfter != stdOffset); // 夏時間開始
                var dstEndRule = ruleList.First(r => r.OffsetAfter == stdOffset);   // 夏時間終了

                fake_posixs += fake_posix(dstStartRule) + ";"; // 夏時間開始（M10）
                fake_posixs += fake_posix(dstEndRule) + ";";   // 夏時間終了（M4）

                Regex regex = new Regex(@"(;(.*?),|;$)");
                fake_posixs = regex.Replace(fake_posixs, ",");

                fk = $",{fake_posixs},";

                regex = new Regex(@"(,$)");
                fake_posixs = "\r\n#java has no regular posix,has binary simpleformat rules..\r\nthis fake_posixs:" + regex.Replace(fake_posixs, "");
                sb.AppendLine(fake_posixs);
            }

            textBox3.Text = sb.ToString();


            //https://github.com/openjdk/jdk/blob/master/src/java.base/share/classes/java/util/SimpleTimeZone.java#L674
            var zz = ZoneInfos.GetZoneInfoInternal( //旧javaapi のsun.util.calendar.ZoneInfo　のメソッドを移植
                zoneInfo.ZoneId,
                zoneInfo.StdTrans,
                zoneInfo.StdOffsets,
                zoneInfo.SavTrans,
                zoneInfo.SavOffsets,
                zoneInfo.Rules // 型が一致するはず
            );

            var zzz = zz.ZoneId;
            //https://github.com/openjdk/jdk/blob/4a50778a2614a69dabf45fbdd57c0226f95a7f6a/src/java.base/share/classes/sun/util/calendar/ZoneInfo.java#L239
            //un.util.calendar transitionsが<<12されるので、逆変換される


            string data = time(zoneInfo.SavTrans, zoneInfo.SavOffsets, zoneInfo.Rules.Length);
            if (data != "MAX")
            {
                time_date.Text = data;
                return;
            }

            setRawOffset(0);
            rawOffset = zz.RawOffset;
            DateTime date = DateTime.Now;
            long milliseconds = new DateTimeOffset(date).ToUnixTimeMilliseconds();
            //java.timeではなく旧javaapiのsun.util.calendar.ZoneInfoを使用しているので使わない
            //longにねじ込むため　trandi*1000<<12で制度落ちしているので使うのが（）
            //int result = GetOffsets(milliseconds, zz.Offsets, 0, zz.Transitions, zz.Offsets, zz.Rules);
            data = get_rule_offset(zz);

            time_date.Text = data;
        }

        string fk = "";

        private string get_rule_offset(ZoneInfos tz)
        {
            int make_year_len = 2;//
            if (comboBox1.SelectedIndex > 0)
            {
                make_year_len = Convert.ToInt32(comboBox1.Text);
            }
            long[] SavTrans = new long[tz.Rules.Length * make_year_len + 1];
            int[] SavOffsets = new int[tz.Rules.Length * make_year_len + 1];
            try
            {
                DateTime date;
                string dd = Properties.Settings.Default.testdt;
                // UTC時間をUnixタイムスタンプ（秒）に変換
                long unixTimestamp = 0;
                if (!DateTime.TryParse(dd, out date))
                {
                    unixTimestamp = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

                }
                else
                {
                    unixTimestamp = ((DateTimeOffset)date).ToUnixTimeSeconds();
                }

                string tzst = tzdb_names.Text;
                string tmp = "";
                bool ch = div3600.Checked;

                var i = 0;
                int thisyear = DateTime.Now.Year;
                const long MinSeconds = -62135596800L;
                const long MaxSeconds = 253402300799L;

                SavTrans[SavTrans.Length - 1] = MaxSeconds;
                SavOffsets[0] = tz.Rules[0].StandardOffset;

                textBox3.Text += $"\r\nrecent year transition\r\n";

                string zz = tz.ZoneId;

                ///** The local date-time is expressed in terms of the UTC offset. */
                //UTC,
                ///** The local date-time is expressed in terms of the wall offset. */
                //WALL,
                ///** The local date-time is expressed in terms of the standard offset. */
                //STANDARD;

                textBox3.Text += $"UTC,WALL\r\n";
                for (int year = thisyear - make_year_len / 2; year <= thisyear + make_year_len / 2 - 1; year++)
                {
                    foreach (var rr in tz.Rules)
                    {
                        // Convert SecondOfDay (in seconds) to a TimeSpan
                        TimeSpan timeOfDay = TimeSpan.FromSeconds(rr.SecondOfDay);

                        // Extract hours, minutes, and seconds
                        int hours = timeOfDay.Hours;
                        int minutes = timeOfDay.Minutes;
                        int seconds = timeOfDay.Seconds;

                        // Start with local standard time
                        DateTime dtLocal = new DateTime(year, (int)rr.Month, (int)rr.DayOfMonth, hours, minutes, seconds);
                        DayOfWeek todayDowEnum = dtLocal.DayOfWeek;
                        int todayDowNumber = (int)todayDowEnum;
                        int dow = rr.DayOfWeek % 7;
                        int diff = (-todayDowNumber + dow) % 7;
                        if (diff < 0) { diff += 7; }
                        dtLocal = dtLocal.AddDays(diff);



                        // Adjust based on TimeDefinition
                        long transitionEpochSec;
                        if (rr.TimeDefinition == 0) // UTC
                        {
                            transitionEpochSec = new DateTimeOffset(dtLocal, TimeSpan.Zero).ToUnixTimeSeconds();
                        }                       
                        else  // Wall 1
                        {
                            int offset = rr.offsetBefore;
                            if (rr.TimeDefinition == 2) // Standard (2)
                            {
                                if (rr.offsetAfter < rr.offsetBefore)
                                {
                                    offset += -rr.OffsetBefore + rr.standardOffset;
                                }
                            }

                            TimeSpan offsetbf = TimeSpan.FromSeconds(offset);

                            transitionEpochSec = new DateTimeOffset(dtLocal, offsetbf).ToUnixTimeSeconds();
                        }

                        //ローカル.,utc 標準に変換
                        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds (transitionEpochSec).ToOffset(TimeSpan.FromSeconds(Convert.ToDouble(rr.OffsetAfter)));
                        if (dateTimeOffset.Month > (int)rr.Month) { 
                            transitionEpochSec -= 3600*24*7;
                        }
                        if (dateTimeOffset.Month < (int)rr.Month) { 
                            transitionEpochSec += 3600 * 24 * 7; 
                        }

                        SavTrans[i] = transitionEpochSec;
                        SavOffsets[i + 1] = rr.OffsetAfter;
                        dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(transitionEpochSec);
                        DateTime utcDateTime = dateTimeOffset.UtcDateTime;
                        string ut = (utcDateTime.ToString("u")); // "u" フォーマットは UTC 時刻を表示
                        string wall = dateTimeOffset.ToOffset(TimeSpan.FromSeconds(Convert.ToDouble(rr.OffsetAfter))).ToString("yyyy-MM-ddTHH:mm:sszzz");

                        textBox3.Text += $"{ut.Replace(" ", "T")},{wall}\r\n";
                        //if (i == 2 || i==3)
                        //{
                        //  textBox4.Text += $"{zz},{fk},{rr.TimeDefinition},{ut.Replace(" ","T")},{wall},{(double)SavOffsets[i + 1] / 3600}\r\n";
                        //}
                        i++;
                    }
                }


                Array.Sort(SavTrans, SavOffsets);
                Array.Resize(ref SavTrans, i);

                int svlen = SavTrans.Length;
                if (svlen < 2)
                {
                    int set_index = 0;
                    if (svlen == 1)
                    {
                        set_index = 1;
                    }
                    double offsetSeconds = Convert.ToDouble(SavOffsets[set_index]);
                    int offsetSeconds_i = SavOffsets[set_index];
                    var offset_st = (offsetSeconds_i / 3600).ToString("D2") + ":" + ((offsetSeconds_i % 3600) / 60).ToString("D2");

                    DateTimeOffset localTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).AddSeconds(offsetSeconds);
                    tmp = localTime.ToString("yyyy-MM-ddTHH:mm:ss+" + offset_st).Replace("+-", "-");
                    tmp = tmp + $"\r\nindex:0\r\ntradition:null\r\noffset0:{SavOffsets[set_index]}";

                    return tmp;

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
                    index = -(index + 1) - 1;
                }
                if (index < 0)
                {
                    index = -1;
                }

                if (index >= -1 && index < SavOffsets.Length)
                {
                    double offsetSeconds = Convert.ToDouble(SavOffsets[index + 1]);
                    int offsetSeconds_i = SavOffsets[index + 1];
                    var offset_st = (offsetSeconds_i / 3600).ToString("D2") + ":" + ((offsetSeconds_i % 3600) / 60).ToString("D2");

                    DateTimeOffset localTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).AddSeconds(offsetSeconds);
                    tmp = localTime.ToString("yyyy-MM-ddTHH:mm:ss+" + offset_st).Replace("+-", "-");
                }

                long trans = 0;
                if (index >= 0) { trans = SavTrans[index]; }
                else { trans = MinSeconds; }

                var tt = "";
                if (ch)
                {
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
                offsetSeconds_ii = offsetSeconds_ii / 3600;



                tmp = tmp + $"\r\nindex+1:{index + 1}\r\ntradition:{tt}\r\noffset+1:{offsetSeconds_ii}";

                return tmp;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private string fake_posix(ZoneOffsetTransitionRule rr)
        {
            int diff = rr.OffsetAfter - rr.OffsetBefore;
            if (diff < 0) { diff = -diff; }
            string std = tohhmm(rr.StandardOffset, 0); // e.g., "12:45"
            string dst = tohhmm(rr.StandardOffset, diff); // e.g., "13:45"


            // Convert SecondOfDay (in seconds) to a TimeSpan
            TimeSpan timeOfDay = TimeSpan.FromSeconds(rr.SecondOfDay);

            // Extract hours, minutes, and seconds
            int hours = timeOfDay.Hours;
            int minutes = timeOfDay.Minutes;
            int seconds = timeOfDay.Seconds;

            int thisyear = DateTime.Now.Year;
            // Start with local standard time
            DateTime dtLocal = new DateTime(thisyear, (int)rr.Month, (int)rr.DayOfMonth, hours, minutes, seconds);
            DayOfWeek todayDowEnum = dtLocal.DayOfWeek;
            int todayDowNumber = (int)todayDowEnum;
            int dow = rr.DayOfWeek % 7;
            int diffs = (-todayDowNumber + dow) % 7;
            if (diffs < 0) { diff += 7; }
            dtLocal = dtLocal.AddDays(diff);
            if (dtLocal.Month > (int)rr.Month) { dtLocal = dtLocal.AddDays(-7); }
            if (dtLocal.Month < (int)rr.Month) { dtLocal = dtLocal.AddDays(7); }

            int offset = rr.SecondOfDay;
            if (rr.TimeDefinition == 0) // UTC only
            {
                offset += rr.OffsetBefore;
            }
            if (rr.TimeDefinition == 2) // std only
            {
                offset += rr.OffsetBefore - rr.StandardOffset;
            }


            // Remove Wall time adjustment for DST end, POSIX uses standard time
            TimeSpan ts = TimeSpan.FromSeconds(offset);
            string tt = ts.ToString(@"h\:mm").Replace(":00", "");

            string fake_posix = "STD{std}DST{dst},M{sm}.{sd}.{sw}/{sh}";
            fake_posix = fake_posix.Replace("{std}", "-" + std);
            fake_posix = fake_posix.Replace("{dst}", "-" + dst);
            fake_posix = fake_posix.Replace("{sm}", rr.Month.ToString());
            if (rr.DayOfMonth >= 23) { fake_posix = fake_posix.Replace("{sd}", "5"); } // Last week
            else { fake_posix = fake_posix.Replace("{sd}", (rr.DayOfMonth / 7 + 1).ToString()); } // Week number
            fake_posix = fake_posix.Replace("{sw}", (rr.DayOfWeek % 7).ToString());
            fake_posix = fake_posix.Replace("{sh}", tt);
            fake_posix = fake_posix.Replace("--", "");
            if (std.Contains(":") || dst.Contains(":"))
            {
                fake_posix = fake_posix.Replace("STD", "<+" + std + ">").Replace("DST", "<+" + dst + ">");
            }
            fake_posix = fake_posix.Replace("+-", "+").Replace("+-", "-");
            return fake_posix;
        }

        private string tohhmm(int dd, int timeDefinition)
        {
            dd = dd + timeDefinition;
            string sigh = "";
            if (dd < 0)
            {
                sigh = "-";
            }
            sigh = sigh + Math.Abs((dd) / 3600).ToString("");
            if (dd % 3600 > 0)
            {
                sigh += ":" + Math.Abs((dd % 3600) / 60).ToString("D2");
            }
            return sigh;
        }



        int rawOffsetDiff = 0;
        int rawOffset = 0;

        private int getLastRawOffset()
        {
            return rawOffset + rawOffsetDiff;
        }

        private int GetLastRawOffset(long[] Transitions)
        {
            return (int)Transitions[Transitions.Length - 1];
        }

        public void setRawOffset(int offsetMillis)
        {
            if (offsetMillis == rawOffset + rawOffsetDiff)
            {
                return;
            }
            rawOffsetDiff = offsetMillis - rawOffset;
            //if (lastRule != null)
            //{
            //     lastRule.setRawOffset(offsetMillis);
            //}
            //dirty = true;
        }

        public int GetOffset(long date)
        {
            return 0;
        }

        // --- 移植されたメソッド ---

        // getOffsets の C# 版
        // date は ミリ秒単位のエポック時間 (UTC) を期待
        // outputOffsets は int[2] 配列で、[0]にRaw Offset(ms), [1]にDST Offset(ms) を格納
        // type は UTC_TIME, WALL_TIME, STANDARD_TIME のいずれか
        public int GetOffsets(long date, int[]? outputOffsets, int type, long[] Transitions, int[] Offsets, ZoneOffsetTransitionRule[] rules)
        {

            /**
             * Difference in milliseconds from the original GMT offset in case
             * the raw offset value has been modified by calling {@link
             * #setRawOffset}. The initial value is 0.
             * @serial
             */
            // transitions 配列がない場合 (DST が観測されないなど)
            if (Transitions == null || Offsets == null) // Offsets もチェック
            {
                int offset = GetLastRawOffset(Transitions);
                if (outputOffsets != null && outputOffsets.Length >= 2)
                {
                    outputOffsets[0] = offset;
                    outputOffsets[1] = 0; // DST は 0
                }
                return offset; // Raw offset のみを返す
            }

            // date の調整 (Javaコードに合わせるが、rawOffsetDiff の意味を確認)
            date -= rawOffsetDiff; // date はミリ秒単位

            int index = GetTransitionIndex(date, type, Transitions, Offsets); // 遷移テーブルのインデックスを検索

            // 最初の遷移よりも前の場合 (FIXME: LMTサポートのコメントは無視)
            if (index < 0)
            {
                int offset = GetLastRawOffset(Transitions);
                if (outputOffsets != null && outputOffsets.Length >= 2)
                {
                    outputOffsets[0] = offset;
                    outputOffsets[1] = 0;
                }
                return offset;
            }

            // 遷移テーブルの範囲内の場合
            if (index < Transitions.Length)
            {
                long val = Transitions[index];
                // Packed データからオフセットインデックスを取得し、オフセット配列から値を取得 (ms 単位)
                // rawOffsetDiff を加算する (Javaコードに合わせる)
                int offsetIndex = (int)(val & OFFSET_MASK);
                int offset = Offsets[offsetIndex] + rawOffsetDiff;

                if (outputOffsets != null && outputOffsets.Length >= 2)
                {
                    // Packed データから DST インデックスを取得
                    int dst = (int)((val >> DST_NSHIFT) & 0xfL); // C# 9.0+ の >>> 相当 (unsigned shift)
                                                                 // DST インデックスを使って DST オフセットを取得 (0なら DST なし)
                    int save = (dst == 0) ? 0 : Offsets[dst]; // DST offset in ms
                    outputOffsets[0] = offset - save; // Raw offset part
                    outputOffsets[1] = save;        // DST offset part
                }
                return offset; // Total offset (Raw + DST)
            }

            // 遷移テーブルの範囲を超えた場合
            // getLastRule() が SimpleTimeZone 相当のルールを返すか確認
            //SimpleTimeZoneEmulator? tz = GetLastRule(); // SimpleTimeZone のエミュレーションが必要
            int tz = rules.Length;
            if (tz > 0)
            {
                int rawoffset = rawOffset; // SimpleTimeZone の Raw Offset (ms)
                long msec = date; // ミリ秒単位のまま

                // type が UTC でなければ、入力時刻を調整
                // Java コードでは rawOffset を引いているが、ZoneInfo の rawOffset か、
                // SimpleTimeZone の rawoffset か要確認。ここでは ZoneInfo のを使うと仮定。
                if (type != UTC_TIME)
                {
                    msec -= rawOffset;
                }

                // SimpleTimeZone 相当のロジックで DST オフセットを取得
                int dstoffset = GetOffset(msec) - rawoffset; // DST offset in ms

                // 標準時から夏時間への遷移チェック (WALL_TIME の場合)
                // このロジックは SimpleTimeZone の getOffset の正確な動作に依存
                if (type == WALL_TIME && dstoffset > 0 && GetOffset(msec - dstoffset) == rawoffset)
                {
                    // ギャップの場合（存在しない時刻）、DST オフセットを 0 にする？ (要 SimpleTimeZone 仕様確認)
                    dstoffset = 0;
                }

                if (outputOffsets != null && outputOffsets.Length >= 2)
                {
                    outputOffsets[0] = rawoffset;
                    outputOffsets[1] = dstoffset;
                }
                return rawoffset + dstoffset; // Total offset
            }
            else
            {
                // SimpleTimeZone ルールがない場合は、最後の遷移ルールを使用
                long val = Transitions[Transitions.Length - 1];
                int offsetIndex = (int)(val & OFFSET_MASK);
                int offset = Offsets[offsetIndex] + rawOffsetDiff;

                if (outputOffsets != null && outputOffsets.Length >= 2)
                {
                    int dst = (int)((val >> DST_NSHIFT) & 0xfL);
                    int save = (dst == 0) ? 0 : Offsets[dst];
                    outputOffsets[0] = offset - save;
                    outputOffsets[1] = save;
                }
                return offset;
            }
        }

        // getTransitionIndex の C# 版
        // date は調整済みのミリ秒単位のエポック時間
        // type は UTC_TIME, WALL_TIME, STANDARD_TIME

        int TRANSITION_NSHIFT = 12; // 12-bit shift for transition time
        int DST_NSHIFT = 4; // 4-bit shift for DST index
        long OFFSET_MASK = 0x0fL; // 4-bit mask for offset index

        // --- 定義が必要な Time Type Constants ---
        // SimpleTimeZone の TimeMode に対応 (値は Java の定義に合わせる)
        private const int UTC_TIME = 0;      // Example value getOffset doc.
        private const int WALL_TIME = 2;     // Example value - Verify!
        private const int STANDARD_TIME = 1; // Example value - Verify! Java's was 2 in ZoneOffsetTransitionRule but maybe different for SimpleTimeZone? Check


        private int GetTransitionIndex(long date, int type, long[] Transitions, int[] Offsets)
        {
            // Transitions または Offsets が null または空の場合は検索できない
            if (Transitions == null || Transitions.Length == 0 || Offsets == null)
            {
                return -1; // または適切なエラー/デフォルト値
            }

            int low = 0;
            int high = Transitions.Length - 1;

            while (low <= high)
            {
                // オーバーフローを避けるため中央値を計算
                int mid = low + ((high - low) / 2);
                long val = Transitions[mid]; // Packed transition data
                                             // 遷移時刻 (ミリ秒) を抽出 (符号拡張を維持するため signed shift >>)
                long midVal = val >> TRANSITION_NSHIFT;

                // 時刻タイプに応じて比較対象の遷移時刻 (midVal) を調整
                if (type != UTC_TIME) // WALL_TIME または STANDARD_TIME
                {
                    int offsetIndex = (int)(val & OFFSET_MASK);
                    // Check if offsetIndex is valid before accessing Offsets array
                    if (offsetIndex >= 0 && offsetIndex < Offsets.Length)
                    {
                        midVal += Offsets[offsetIndex]; // Wall time にする
                    }
                    else
                    {
                        // Handle error: invalid offset index in transition data
                        // For safety, perhaps break or return an error indicator
                        return -1; // Or throw exception
                    }

                }
                if (type == STANDARD_TIME)
                {
                    int dstIndex = (int)((val >> DST_NSHIFT) & 0xfL); // Unsigned shift
                    if (dstIndex != 0)
                    {
                        // Check if dstIndex is valid before accessing Offsets array
                        if (dstIndex >= 0 && dstIndex < Offsets.Length)
                        {
                            midVal -= Offsets[dstIndex]; // Standard time にする (DST分を引く)
                        }
                        else
                        {
                            // Handle error: invalid DST index
                            return -1; // Or throw exception
                        }

                    }
                }

                // バイナリサーチの比較
                if (midVal < date)
                {
                    low = mid + 1;
                }
                else if (midVal > date)
                {
                    high = mid - 1;
                }
                else
                {
                    // 正確に一致した場合
                    return mid;
                }
            }

            // 完全に一致しなかった場合
            // low は date より大きい最初の要素のインデックス、または配列サイズ
            // high は date より小さい最後の要素のインデックス

            // 最後の遷移よりも未来の場合、最後のインデックスを返す (ように見えるが low を返す?)
            // Java版は low >= length の場合 low を返し、それ以外は low - 1 を返す。
            if (low >= Transitions.Length)
            {
                return low; // Java版の挙動に合わせる (範囲外を示すインデックス)
            }
            // date が transitions[low-1] と transitions[low] の間にある場合
            return low - 1; // date が適用されるルールのインデックス (直前の遷移)
        }

        public class ZoneInfos
        {
            public string ZoneId { get; }
            public int RawOffset { get; } // Milliseconds
            public int DstSavings { get; } // Milliseconds
            public int Checksum { get; }
            public long[]? Transitions { get; } // Nullable if no transitions
            public int[]? Offsets { get; }     // Nullable if no transitions
            public int[]? ParamsArray { get; } // Nullable if no rules for SimpleTimeZone params
            public bool WillGMTOffsetChange { get; }
            public ZoneOffsetTransitionRule[] Rules { get; } // 型を変更

            // Constructor matching the return statement of the ported method
            public ZoneInfos(string zoneId, int rawOffset, int dstSavings, int checksum,
                            long[]? transitions, int[]? offsets, int[]? paramsArray, bool willGMTOffsetChange, ZoneOffsetTransitionRule[] rules) // 型を変更
            {
                ZoneId = zoneId;
                RawOffset = rawOffset;
                DstSavings = dstSavings;
                Checksum = checksum;
                Transitions = transitions;
                Offsets = offsets;
                ParamsArray = paramsArray; // Renamed from 'params' to avoid keyword conflict
                WillGMTOffsetChange = willGMTOffsetChange;
                Rules = rules;
            }

            // --- Add any other methods or properties needed ---


            // --- Define constants ---
            private const long UTC1900 = -2208988800L; // Seconds from Unix epoch for 1900-01-01T00:00:00Z
            private const long UTC2037 = 2145916799L; // Seconds from Unix epoch for 2038-01-01T00:00:00Z - 1
            private const long LDT2037 = 2114380800L; // Seconds from Unix epoch for 2037-01-01T00:00:00Z (approx, may depend on offset)
            private static readonly long CURRT = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // Current UTC epoch seconds
            private const int LASTYEAR = 2037;
            private const long OFFSET_MASK = 0x0fL;
            private const long DST_MASK = 0xf0L;
            private const int DST_NSHIFT = 4;
            private const int TRANSITION_NSHIFT = 12;

            // Example mapping arrays/constants (Adjust values based on Java's Calendar/SimpleTimeZone)
            private static readonly int[] toCalendarDOW = new int[] { -1, 1, 2, 3, 4, 5, 6, 7 }; // Map 1-7 (Mon-Sun) to Java Calendar values (e.g., Calendar.MONDAY=2) - NEEDS VERIFICATION
            private static readonly double[] toSTZTime = new double[] { 0, 1, 2 }; // Map TimeDefinition (0=UTC, 1=WALL, 2=STANDARD) to SimpleTimeZone values - NEEDS VERIFICATION
                                                           


            // --- Ported GetZoneInfo Method ---
            public static ZoneInfos GetZoneInfoInternal(string zoneId,
                                             long[] standardTransitions,
                                             int[] standardOffsets, // Seconds
                                             long[] savingsInstantTransitions, // Epoch Seconds
                                             int[] wallOffsets, // Seconds
                                             ZoneOffsetTransitionRule[] lastRules)
            {
                int rawOffset = 0; // Milliseconds
                int dstSavings = 0; // Milliseconds
                int checksum = 0;
                int[]? paramsArray = null; // Renamed from 'params'
                bool willGMTOffsetChange = false;

                // rawOffset, pick the last one
                if (standardTransitions.Length > 0)
                {
                    // Offsets from TZDB are in seconds, convert to milliseconds for ZoneInfo
                    rawOffset = standardOffsets[standardOffsets.Length - 1] * 1000;
                    willGMTOffsetChange = standardTransitions[standardTransitions.Length - 1] > CURRT;
                }
                else
                {
                    rawOffset = standardOffsets[0] * 1000;
                }

                // transitions, offsets;
                long[]? transitions = null; // Stores combined transition info (packed long)
                int[]? offsets = null;     // Stores unique offset values in milliseconds
                int nOffsets = 0;
                int nTrans = 0;

                if (savingsInstantTransitions.Length != 0)
                {
                    // Initial allocation (can be resized)
                    transitions = new long[250];
                    offsets = new int[100];    // Java comment noted potential 4-bit limit (16 entries) - check ZoneInfo needs

                    int lastyear = GetYear(savingsInstantTransitions[savingsInstantTransitions.Length - 1],
                                            wallOffsets[savingsInstantTransitions.Length - 1]); // GetYear needs porting
                    int i = 0, k = 1; // i for savingsInstantTransitions, k for standardTransitions

                    // Skip transitions before UTC1900
                    while (i < savingsInstantTransitions.Length && savingsInstantTransitions[i] < UTC1900)
                    {
                        i++;
                    }

                    if (i < savingsInstantTransitions.Length)
                    {
                        // javazic writes the last standard GMT offset into index 0!
                        // Offsets array stores values in milliseconds
                        offsets[0] = standardOffsets[standardOffsets.Length - 1] * 1000;
                        nOffsets = 1;

                        // Add the starting entry for UTC1900 if there are other transitions
                        nOffsets = AddTrans(transitions, nTrans++,
                                            offsets, nOffsets,
                                            UTC1900, // Epoch Second
                                            wallOffsets[i], // Wall offset in seconds for the *first valid* transition
                                            GetStandardOffset(standardTransitions, standardOffsets, UTC1900)); // Standard offset in seconds at UTC1900
                    }

                    // Process savings transitions, merging standard transitions
                    for (; i < savingsInstantTransitions.Length; i++)
                    {
                        long trans = savingsInstantTransitions[i]; // Current savings transition time (epoch seconds)
                        if (trans > UTC2037)
                        {
                            lastyear = LASTYEAR; // Don't process beyond LASTYEAR
                            break;
                        }

                        // Merge standard transitions that occur before the current savings transition
                        while (k < standardTransitions.Length)
                        {
                            long trans_s = standardTransitions[k]; // Standard transition time (epoch seconds)
                            if (trans_s >= UTC1900)
                            {
                                if (trans_s > trans) break; // Standard transition is after savings transition

                                if (trans_s < trans) // Standard transition before savings transition
                                {
                                    // Resize arrays if needed
                                    if (nOffsets + 2 >= offsets.Length) Array.Resize(ref offsets, offsets.Length + 100);
                                    if (nTrans + 1 >= transitions.Length) Array.Resize(ref transitions, transitions.Length + 100);

                                    // Add the standard transition
                                    nOffsets = AddTrans(transitions, nTrans++, offsets, nOffsets,
                                                        trans_s,             // Standard transition time
                                                        wallOffsets[i],      // Wall offset *at the time of the next savings transition* (used for DST calculation potentially) - Java version uses wallOffsets[i], verify logic
                                                        standardOffsets[k + 1]); // Standard offset *after* this standard transition
                                }
                                // If trans_s == trans, it will be handled by the savings transition logic below
                            }
                            k++;
                        }

                        // Resize arrays if needed for the current savings transition
                        if (nOffsets + 2 >= offsets.Length) Array.Resize(ref offsets, offsets.Length + 100);
                        if (nTrans + 1 >= transitions.Length) Array.Resize(ref transitions, transitions.Length + 100);

                        // Add the current savings transition
                        nOffsets = AddTrans(transitions, nTrans++, offsets, nOffsets,
                                            trans,              // Savings transition time
                                            wallOffsets[i + 1], // Wall offset *after* this savings transition
                                            GetStandardOffset(standardTransitions, standardOffsets, trans)); // Standard offset *at* this savings transition time
                    }

                    // Append any leftover standard transitions after the last savings transition (or if none existed)
                    while (k < standardTransitions.Length)
                    {
                        long trans_s = standardTransitions[k];
                        if (trans_s >= UTC1900 && trans_s <= UTC2037) // Ensure within bounds
                        {
                            // Resize if needed
                            if (nOffsets + 1 >= offsets.Length) Array.Resize(ref offsets, offsets.Length + 100); // Only need space for potentially one new offset
                            if (nTrans + 1 >= transitions.Length) Array.Resize(ref transitions, transitions.Length + 100);

                            int wallOffsetSeconds = wallOffsets[i]; // Wall offset after the last processed savings transition (or initial if none)
                            int offsetIndex = IndexOf(offsets, 0, nOffsets, wallOffsetSeconds); // Find or add the wall offset (in seconds)
                            if (offsetIndex == nOffsets)
                            {
                                offsets[nOffsets++] = wallOffsetSeconds * 1000; // Add in milliseconds
                            }


                            // Java code only adds (offsetIndex & OFFSET_MASK), implying DST index is 0. Verify this assumption.
                            // Assuming standard transitions don't involve DST changes directly in this encoding.
                            transitions[nTrans++] = ((trans_s * 1000L) << TRANSITION_NSHIFT) | // Transition time in milliseconds
                                                    ((long)offsetIndex & OFFSET_MASK);
                        }
                        k++;
                    }

                    // Process recurring rules (lastRules) to fill up to LASTYEAR
                    if (lastRules.Length > 1)
                    {
                        // Fill gap using rules
                        while (lastyear < LASTYEAR) // Note: Java used lastyear++, check start year logic
                        {
                            lastyear++; // Increment year first to match Java's while loop condition
                            foreach (ZoneOffsetTransitionRule zotr in lastRules)
                            {
                                long trans = zotr.GetTransitionEpochSecond(lastyear); // Needs porting

                                if (trans > UTC2037) continue; // Skip transitions beyond the limit

                                // Resize arrays if needed
                                if (nOffsets + 2 >= offsets.Length) Array.Resize(ref offsets, offsets.Length + 100);
                                if (nTrans + 1 >= transitions.Length) Array.Resize(ref transitions, transitions.Length + 100);

                                nOffsets = AddTrans(transitions, nTrans++, offsets, nOffsets,
                                                    trans,             // Rule transition time (epoch seconds)
                                                    zotr.offsetAfter,  // Offset after rule transition (seconds)
                                                    zotr.standardOffset); // Standard offset during rule (seconds)
                            }
                        }

                        // Generate SimpleTimeZone compatible parameters from the last two rules
                        ZoneOffsetTransitionRule startRule = lastRules[lastRules.Length - 2];
                        ZoneOffsetTransitionRule endRule = lastRules[lastRules.Length - 1];
                        paramsArray = new int[10];

                        // Ensure startRule is the one transitioning *into* DST if applicable
                        if (startRule.offsetAfter - startRule.offsetBefore < 0 &&
                            endRule.offsetAfter - endRule.offsetBefore > 0)
                        {
                            var tmpRule = startRule;
                            startRule = endRule;
                            endRule = tmpRule;
                        }

                        paramsArray[0] = startRule.month - 1; // Java month is 0-based
                        int dom = startRule.dom;
                        int dow = startRule.dow;
                        if (dow == -1)
                        { // Fixed date
                            paramsArray[1] = dom;
                            paramsArray[2] = 0; // SimpleTimeZone: 0 for exact day
                        }
                        else
                        { // Day-of-week rule
                          // Java's SimpleTimeZone uses complex encoding for day-of-week rules
                          // Needs careful mapping based on SimpleTimeZone documentation and toCalendarDOW array
                          // Placeholder logic based on Java comments:
                            if (dom < 0 || dom >= 24)
                            { // Assumed "last week" encoding or similar ZRB optimization
                                paramsArray[1] = -1; // SimpleTimeZone: -1 for last day of month
                                paramsArray[2] = toCalendarDOW[dow]; // Day of week (e.g., Calendar.SUNDAY) - Verify mapping!
                            }
                            else
                            { // Day of week >= dom OR Day of week <= dom
                                paramsArray[1] = dom;
                                paramsArray[2] = -toCalendarDOW[dow]; // SimpleTimeZone: negative for "on or after" - Verify mapping and sign!
                            }
                        }
                        paramsArray[3] = startRule.secondOfDay * 1000; // Milliseconds
                        //paramsArray[4] = toSTZTime[startRule.timeDefinition]; // Map to SimpleTimeZone mode (UTC, WALL, STANDARD) - Verify mapping!

                        // Repeat for endRule
                        paramsArray[5] = endRule.month - 1;
                        dom = endRule.dom;
                        dow = endRule.dow;
                        if (dow == -1)
                        {
                            paramsArray[6] = dom;
                            paramsArray[7] = 0;
                        }
                        else
                        {
                            if (dom < 0 || dom >= 24)
                            {
                                paramsArray[6] = -1;
                                paramsArray[7] = toCalendarDOW[dow]; // Verify mapping!
                            }
                            else
                            {
                                paramsArray[6] = dom;
                                paramsArray[7] = -toCalendarDOW[dow]; // Verify mapping and sign!
                            }
                        }
                        paramsArray[8] = endRule.secondOfDay * 1000; // Milliseconds
                        //paramsArray[9] = toSTZTime[endRule.timeDefinition]; // Verify mapping!

                        // DST Savings in milliseconds
                        dstSavings = (startRule.offsetAfter - startRule.offsetBefore) * 1000;

                        // *** HARDCODED PATCHES EXCLUDED AS REQUESTED ***
                        // The original Java code had specific adjustments here for
                        // Asia/Amman, Asia/Gaza, Asia/Hebron, Africa/Cairo based on params values.
                        // If needed, these would be ported here.

                    }
                    else if (nTrans > 0)
                    { // Has transitions but no recurring rules (or only one rule)
                        if (lastyear < LASTYEAR)
                        {
                            // Add a final transition entry for LASTYEAR limit if needed
                            // Java calculates based on LDT2037 - rawOffset/1000. Verify this logic.
                            // rawOffset is in milliseconds here.
                            long trans = LDT2037 - (rawOffset / 1000L);

                            // Resize if needed
                            if (nOffsets + 1 >= offsets.Length) Array.Resize(ref offsets, offsets.Length + 100);
                            if (nTrans + 1 >= transitions.Length) Array.Resize(ref transitions, transitions.Length + 100);

                            // Find or add the raw offset (in seconds) to the offsets array
                            int offsetIndex = IndexOf(offsets, 0, nOffsets, rawOffset / 1000);
                            if (offsetIndex == nOffsets)
                            {
                                offsets[nOffsets++] = rawOffset; // Add raw offset in milliseconds
                            }

                            // Add final transition, assuming DST index 0 (no DST saving)
                            transitions[nTrans++] = (trans * 1000L << TRANSITION_NSHIFT) | // Transition time in milliseconds
                                                    ((long)offsetIndex & OFFSET_MASK);
                        }
                        else if (savingsInstantTransitions.Length > 2)
                        {
                            // Workaround for zones like Israel/Iran with transitions up to 2037 but no explicit rules
                            // This part reconstructs SimpleTimeZone params from the last two transitions.
                            // Requires careful porting of Java's java.time logic using C#'s DateTimeOffset.

                            int m = savingsInstantTransitions.Length;
                            long startTransEpochSec = savingsInstantTransitions[m - 2];
                            int startWallOffsetSec = wallOffsets[m - 1]; // Offset after the transition
                            int startStdOffsetSec = GetStandardOffset(standardTransitions, standardOffsets, startTransEpochSec);

                            long endTransEpochSec = savingsInstantTransitions[m - 1];
                            int endWallOffsetSec = wallOffsets[m]; // Offset after the transition
                            int endStdOffsetSec = GetStandardOffset(standardTransitions, standardOffsets, endTransEpochSec);

                            // Check if the pattern looks like a DST transition pair (one into DST, one out)
                            if (startWallOffsetSec > startStdOffsetSec && endWallOffsetSec == endStdOffsetSec)
                            {
                                // --- Porting logic for reconstructing paramsArray ---
                                // This involves converting epoch seconds and offsets back to local date/time
                                // using DateTimeOffset.FromUnixTimeSeconds and TimeSpan.FromSeconds.
                                // Be careful with interpreting the wall time vs standard time.
                                // Example Sketch:

                                TimeSpan startOffsetBefore = TimeSpan.FromSeconds(wallOffsets[m - 2]);
                                TimeSpan startOffsetAfter = TimeSpan.FromSeconds(startWallOffsetSec);
                                DateTimeOffset startTransInstant = DateTimeOffset.FromUnixTimeSeconds(startTransEpochSec);
                                DateTimeOffset startLDTInstant = startTransInstant.ToOffset(startOffsetBefore); // Time just before transition
                                                                                                                // Adjust if it's an overlap based on Java logic
                                                                                                                // LocalDateTime startLDT = ... convert startLDTInstant ...

                                TimeSpan endOffsetBefore = TimeSpan.FromSeconds(wallOffsets[m - 1]);
                                TimeSpan endOffsetAfter = TimeSpan.FromSeconds(endWallOffsetSec);
                                DateTimeOffset endTransInstant = DateTimeOffset.FromUnixTimeSeconds(endTransEpochSec);
                                DateTimeOffset endLDTInstant = endTransInstant.ToOffset(endOffsetBefore); // Time just before transition
                                                                                                          // Adjust if it's an overlap based on Java logic
                                                                                                          // LocalDateTime endLDT = ... convert endLDTInstant ...

                                paramsArray = new int[10];
                                paramsArray[0] = startLDTInstant.Month - 1;
                                paramsArray[1] = startLDTInstant.Day;
                                paramsArray[2] = 0; // Fixed date assumed in this workaround
                                paramsArray[3] = (int)startLDTInstant.TimeOfDay.TotalMilliseconds;
                                paramsArray[4] = WALL_TIME; // Assumed WALL_TIME based on Java logic

                                paramsArray[5] = endLDTInstant.Month - 1;
                                paramsArray[6] = endLDTInstant.Day;
                                paramsArray[7] = 0; // Fixed date assumed
                                paramsArray[8] = (int)endLDTInstant.TimeOfDay.TotalMilliseconds;
                                paramsArray[9] = WALL_TIME; // Assumed WALL_TIME

                                dstSavings = (startWallOffsetSec - startStdOffsetSec) * 1000;

                                // --- End Sketch ---
                                Console.WriteLine("WARNING: Porting needed for Israel/Iran workaround in GetZoneInfoInternal.");
                                // Set paramsArray = null or throw if full implementation is not done
                                //paramsArray = null; // Or implement the detailed logic above
                                dstSavings = (startWallOffsetSec - startStdOffsetSec) * 1000;
                            }
                        }
                    }

                    // Trim arrays to actual size
                    if (transitions != null && transitions.Length != nTrans)
                    {
                        if (nTrans == 0) transitions = null;
                        else Array.Resize(ref transitions, nTrans);
                    }
                    if (offsets != null && offsets.Length != nOffsets)
                    {
                        if (nOffsets == 0) offsets = null;
                        else Array.Resize(ref offsets, nOffsets);
                    }

                    // Calculate checksum if transitions exist

                } // End of if (savingsInstantTransitions.Length != 0)

                // Return the new ZoneInfo object with all calculated/processed data
                return new ZoneInfos(zoneId, rawOffset, dstSavings, checksum, transitions,
                                    offsets, paramsArray, willGMTOffsetChange, lastRules);
            }


            // --- Helper Methods Needing Porting ---

            // Ported from Java - searches std transitions for the offset at epochSec
            private static int GetStandardOffset(long[] standardTransitions, int[] standardOffsets, long epochSec)
            {
                int index = 0;
                // Simple linear scan, assuming small array size as per Java comment
                for (; index < standardTransitions.Length; index++)
                {
                    if (epochSec < standardTransitions[index])
                    {
                        break;
                    }
                }
                // Returns offset in SECONDS
                return standardOffsets[index];
            }

            // Ported from Java - Finds index of offset (in seconds) or adds it (updates nOffsets). Returns index.
            // Modifies the offsets array directly! Offsets array stores values in MILLISECONDS.
            private static int IndexOf(int[] offsets, int fromIndex, int currentNOffsets, int offsetSeconds)
            {
                int offsetMillis = offsetSeconds * 1000;
                for (int i = fromIndex; i < currentNOffsets; i++)
                {
                    if (offsets[i] == offsetMillis)
                        return i;
                }
                // Not found - caller should ensure space and increment nOffsets *after* calling this if index == currentNOffsets
                // offsets[currentNOffsets] = offsetMillis; // Caller adds the value
                return currentNOffsets; // Return the index where it *should* be added
            }

            // Ported from Java - Adds transition info to transitions array, updates offsets array if needed.
            // Returns the *new* count of unique offsets (nOffsets).
            // Modifies transitions and offsets arrays!
            private static int AddTrans(long[] transitions, int nTrans,
                                        int[] offsets, int nOffsets,
                                        long transEpochSec, int wallOffsetSeconds, int stdOffsetSeconds)
            {
                // Find index for the wall offset (offset after transition)
                int wallOffsetIndex = IndexOf(offsets, 0, nOffsets, wallOffsetSeconds);
                if (wallOffsetIndex == nOffsets)
                {
                    // Add new wall offset (in milliseconds)
                    offsets[nOffsets++] = wallOffsetSeconds * 1000;
                }


                int dstIndex = 0;
                int dstSavingsSeconds = wallOffsetSeconds - stdOffsetSeconds;
                if (dstSavingsSeconds != 0)
                {
                    // Find index for the DST savings amount
                    // Java ZoneInfo uses indices from 1 for DST savings? Check this logic.
                    // IndexOf searches from index 1 for DST savings based on Java code comment interpretation.
                    dstIndex = IndexOf(offsets, 1, nOffsets, dstSavingsSeconds);
                    if (dstIndex == nOffsets)
                    {
                        // Add new DST savings offset (in milliseconds)
                        offsets[nOffsets++] = dstSavingsSeconds * 1000;
                    }
                }

                // Pack the transition info into a long
                // Transition time needs to be milliseconds for packing
                transitions[nTrans] = ((transEpochSec * 1000L) << TRANSITION_NSHIFT) |
                                      (((long)dstIndex << DST_NSHIFT) & DST_MASK) |
                                      ((long)wallOffsetIndex & OFFSET_MASK);

                return nOffsets; // Return the updated count of offsets
            }

            // Ported from Java - Calculates year from epoch seconds and offset
            // Requires constants: SECONDS_PER_DAY, DAYS_PER_CYCLE, DAYS_0000_TO_1970
            private static readonly int SECONDS_PER_DAY = 86400;
            private static readonly int DAYS_PER_CYCLE = 146097; // 400 years
            private static readonly long DAYS_0000_TO_1970 = (DAYS_PER_CYCLE * 5L) - (30L * 365L + 7L); // 719162

            private static int GetYear(long epochSecond, int offsetSeconds)
            {
                long second = epochSecond + offsetSeconds;
                long epochDay = Math.DivRem(second, SECONDS_PER_DAY, out long rem);
                if (rem < 0)
                { // Adjust for negative remainders if Math.DivRem doesn't match floorDiv exactly
                    epochDay--;
                }
                // Rest of the calculation from Java's getYear using epochDay
                // ... (port the logic involving zeroDay, adjust, yearEst, etc.) ...
                // This date calculation is complex and needs careful porting.
                // Simplified placeholder using BCL (may not match exact leap year/cutoff logic):
                try
                {
                    return DateTimeOffset.FromUnixTimeSeconds(epochSecond).AddSeconds(offsetSeconds).Year;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Handle cases outside DateTimeOffset range if necessary, potentially returning min/max year
                    // Or implement the full complex calculation from Java
                    return (epochSecond > 0) ? 9999 : 1; // Very basic placeholder
                }
                // throw new NotImplementedException("Port GetYear accurately");
            }



        }

        private void test_datetime_TextChanged(object sender, EventArgs e)
        {
            string st = test_datetime.Text;
            DateTime date;
            if (DateTime.TryParse(st, out date))
            {
                Properties.Settings.Default.testdt = st;

            }
            if (st.Length == 0)
            {
                Properties.Settings.Default.testdt = "";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            tzdb_names.Text = Properties.Settings.Default.tzst;
            test_datetime.Text = Properties.Settings.Default.testdt;
        }
    }
}