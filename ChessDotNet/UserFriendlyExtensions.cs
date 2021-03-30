using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChessDotNet
{
    public static class UserFriendlyExtensions
    {
        public static string TrimNumber(long number, double divisor, string sufffix)
        {
            var divided = number / divisor;
            var str = divided.ToString(CultureInfo.InvariantCulture);
            if (str.Contains("."))
            {
                var newLength = str.Length < 5 ? str.Length : 5;
                str = str.Substring(0, newLength).TrimEnd('0').TrimEnd('.');
            }
            else
            {
                var newLength = str.Length < 4 ? str.Length : 4;
                str = str.Substring(0, newLength);
            }

            return str + sufffix;
        }

        public static string ToUserFriendly(this long number)
        {
            // This doesn't round numbers, just floors them
            var numberStr = number.ToString();
            string suffix;
            switch (numberStr.Length)
            {
                case 1:
                case 2:
                case 3:
                    return numberStr;
                case 4:
                case 5:
                case 6:
                    return TrimNumber(number, 1000, "k");
                case 7:
                case 8:
                case 9:
                    return TrimNumber(number, 1000000, "M");
                case 10:
                case 11:
                case 12:
                    return TrimNumber(number, 1000000000, "B");
                case 13:
                case 14:
                case 15:
                    return TrimNumber(number, 1000000000000, "T");
                default:
                    return numberStr;
            }
        }
    }
}
