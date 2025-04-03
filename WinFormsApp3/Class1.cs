using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp3
{

    // これは WinFormsApp3.ZoneOffsetTransitionRule クラスに追加するメソッド群です
    public class ZoneOffsetTransitionRule
    {
        int SECONDS_PER_DAY = 86400;
        int DAYS_PER_CYCLE = 146097;
        long DAYS_TO_1970 = (146097 * 5L) - (30L * 365L + 7L);

        // --- 既存のフィールド (month, dom, dow, secondOfDay, timeDefinition, etc.) ---
        public int month;
        public byte dom;
        public int dow;
        public int secondOfDay;
        public bool timeEndOfDay; // <--- このフィールドを追加し、コンストラクタで初期化
        public int timeDefinition;
        public int standardOffset;
        public int offsetBefore;
        public int offsetAfter;

        // --- 既存のコンストラクタ (timeEndOfDay の初期化を追加) ---
        public ZoneOffsetTransitionRule(byte[] bs, ref int pointer)
        {
            int data = BinaryPrimitives.ReadInt32BigEndian(bs.AsSpan(pointer));
            pointer += 4;

            int dowByte = (data & (7 << 19)) >>> 19; // C# 9.0+ required for >>>
            int timeByte = (data & (31 << 14)) >>> 14;
            int stdByte = (data & (255 << 4)) >>> 4;
            int beforeByte = (data & (3 << 2)) >>> 2;
            int afterByte = (data & 3);

            this.month = (int)((uint)data >> 28); // Use unsigned shift for month
            this.dom = (byte)(((data & (63 << 22)) >>> 22) - 32);
            this.dow = dowByte == 0 ? -1 : dowByte;

            // timeEndOfDay をここで設定
            this.timeEndOfDay = timeByte == 24;

            this.secondOfDay = timeByte == 31 ? BinaryPrimitives.ReadInt32BigEndian(bs.AsSpan(pointer)) : timeByte * 3600;
            if (timeByte == 31) pointer += 4; // ポインタを進める

            this.timeDefinition = ((data & (3 << 12)) >>> 12);


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

        // *** 移植された GetTransitionEpochSecond ***
        public long GetTransitionEpochSecond(int year)
        {
            long epochDay = 0;
            if (dom < 0)
            {
                // 月末からの相対日 (-1 は最終日, -2 は最終日の前日)
                epochDay = ToEpochDay(year, month, LengthOfMonth(year, month) + 1 + dom);
                if (dow != -1)
                {
                    // 指定された曜日になるように調整 (指定日以前の直近の曜日)
                    epochDay = PreviousOrSame(epochDay, dow);
                }
            }
            else
            {
                // 月の特定の日
                epochDay = ToEpochDay(year, month, dom);
                if (dow != -1)
                {
                    // 指定された曜日になるように調整 (指定日以降の直近の曜日)
                    epochDay = NextOrSame(epochDay, dow);
                }
            }

            // timeEndOfDay フラグをチェック (Java コードに基づき追加)
            if (timeEndOfDay)
            {
                epochDay += 1;
            }

            int difference = 0;
            switch (timeDefinition) // timeDefinition の値は Java の定義に合わせる
            {
                case 0: // UTC (SimpleTimeZone.UTC_TIME に相当)
                    difference = 0;
                    break;
                case 1: // WALL (SimpleTimeZone.WALL_TIME に相当)
                    difference = -offsetBefore; // Offset は秒単位
                    break;
                case 2: // STANDARD (SimpleTimeZone.STANDARD_TIME に相当)
                    difference = -standardOffset; // Offset は秒単位
                    break;
            }
            // 結果はエポック秒 (UTC 1970-01-01T00:00:00Z からの秒数)
            return epochDay * SECONDS_PER_DAY + secondOfDay + difference;
        }

        // --- 以下、GetTransitionEpochSecond に必要なヘルパーメソッド ---

        // Java の Math.floorMod 相当
        private static long FloorMod(long x, long y)
        {
            long r = x % y;
            if ((r ^ y) < 0 && r != 0) // Java版のMath.floorModの挙動を模倣
            {
                r += y;
            }
            return r;
        }

        // C# の % 演算子は Java の Math.floorMod と挙動が違うため注意
        // (特に負数で)。上記 FloorMod ヘルパーを使用。
        private static int FloorMod(int x, int y)
        {
            int r = x % y;
            if ((r ^ y) < 0 && r != 0)
            {
                r += y;
            }
            return r;
        }


        // isLeapYear (static)
        public static bool IsLeapYear(int year)
        {
            // ISO 8601 / Proleptic Gregorian leap year rule
            return ((year & 3) == 0) && ((year % 100) != 0 || (year % 400) == 0);
        }

        // lengthOfMonth (static)
        public static int LengthOfMonth(int year, int month) // month は 1-12
        {
            switch (month)
            {
                case 2: // February
                    return IsLeapYear(year) ? 29 : 28;
                case 4: // April
                case 6: // June
                case 9: // September
                case 11: // November
                    return 30;
                default: // Jan, Mar, May, Jul, Aug, Oct, Dec
                    return 31;
            }
        }
        private static long FloorDiv(long x, long y)
        {
            long r = x / y;
            // If the signs are different and there's a remainder, adjust result downwards
            if ((x ^ y) < 0 && (r * y != x))
            {
                r--;
            }
            return r;
        }
        // toEpochDay (static)
        // Form1 クラスまたはアクセス可能な場所に DAYS_0000_TO_1970 定数が必要
        public static long ToEpochDay(int year, int month, int day) // month は 1-12, day は 1-31
        {
            long y = year;
            long m = month;
            long total = 0;
            total += 365 * y;
            if (y >= 0)
            {
                // Use FloorDiv for calculations involving division where rounding towards negative infinity is needed
                total += FloorDiv(y + 3, 4) - FloorDiv(y + 99, 100) + FloorDiv(y + 399, 400);
            }
            else
            {
                // Handle negative years, using FloorDiv implicitly via FloorDiv helper function logic
                total -= FloorDiv(y, -4) - FloorDiv(y, -100) + FloorDiv(y, -400); // Check this logic carefully for negative years
            }
            total += ((367 * m - 362) / 12); // Standard division likely ok here
            total += day - 1;
            if (m > 2) // Adjust for leap year day if month is after February
            {
                total--;
                if (!IsLeapYear(year))
                {
                    total--;
                }
            }

            long DAYS_TO_1970 = (146097 * 5L) - (30L * 365L + 7L);

            return total - DAYS_TO_1970; // Subtract days from 0000 to 1970 epoch
        }

        // previousOrSame (static)
        // dow は 1(Mon)から7(Sun)を期待している (Java版の toCalendarDOW の使い方に依存)
        public static long PreviousOrSame(long epochDay, int dow)
        {
            return Adjust(epochDay, dow, 1); // relative=1 for previous
        }

        // nextOrSame (static)
        // dow は 1(Mon)から7(Sun)を期待している (Java版の toCalendarDOW の使い方に依存)
        public static long NextOrSame(long epochDay, int dow)
        {
            return Adjust(epochDay, dow, 0); // relative=0 for next
        }

        // adjust (static)
        // dow は 1(Mon)から7(Sun)を期待している
        private static long Adjust(long epochDay, int dow, int relative)
        {
            // Calculate current day-of-week assuming epochDay 0 (1970-01-01) was a Thursday (day 4 if Mon=1)
            // Java: Math.floorMod(epochDay + 3, 7) -> 0 (Mon) .. 6 (Sun)
            // C# : FloorMod(epochDay + 3, 7) -> 0 (Mon) .. 6 (Sun)
            int calDow = (int)FloorMod(epochDay + 3, 7L) + 1; // 1 (Mon) .. 7 (Sun)

            if (relative < 2 && calDow == dow)
            {
                return epochDay; // Already the correct day of week
            }

            if ((relative & 1) == 0) // nextOrSame (relative=0)
            {
                int daysDiff = calDow - dow; // How many days calDow is ahead of target dow (can be negative)
                                             // Add days to reach the *next* target dow
                return epochDay + (daysDiff >= 0 ? 7 - daysDiff : -daysDiff);
            }
            else // previousOrSame (relative=1)
            {
                int daysDiff = dow - calDow; // How many days target dow is ahead of calDow (can be negative)
                                             // Subtract days to reach the *previous* target dow
                return epochDay - (daysDiff >= 0 ? 7 - daysDiff : -daysDiff);
            }
        }

    } // --- End of ZoneOffsetTransitionRule class ---

}
