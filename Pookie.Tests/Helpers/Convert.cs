using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AFUT.Tests.Helpers
{
    public static class Convert
    {
        public static T ToType<T>(this string value)
        {
            return (T)ToType(value, typeof(T));
        }

        public static string FromType<T>(this T value)
        {
            return value switch
            {
                DateTime date => date.ToShortDateString(),
                null => null,
                _ => value.ToString(),
            };
        }

        public static object ToType(string value, Type type)
        {
            if (value == null)
            {
                return default;
            }

            if (type == typeof(string))
            {
                return value;
            }
            if (Converters.ContainsKey(type))
            {
                return Converters[type](value);
            }

            return System.Convert.ChangeType(value, type);
        }

        private static readonly Dictionary<Type, Func<string, object>> Converters = new Dictionary<Type, Func<string, object>>()
        {
            { typeof(DateTime), (s) => DateTime.Parse(s) },
            {typeof(DateTime?), (s) => DateTime.TryParse(s, out var v) ? v : (DateTime?)null},
            {typeof(Guid), (s) => Guid.Parse(s)},
            {typeof(Guid?), (s) => Guid.TryParse(s, out var v) ? v :(Guid?)null},
            {typeof(int), (s) => int.Parse(s)},
            {typeof(int?), (s) => int.TryParse(s, out var v) ? v : (int?)null },
            {typeof(bool), (s) => bool.Parse(s)},
            { typeof(bool?), (s) => HandleNullBools(s, out var v) ? v : (bool?) null},
            {typeof(decimal), (s) => decimal.Parse(s)},
            {typeof(decimal?), (s) => decimal.TryParse(s, out var v) ? v : (decimal?)default },
            {typeof(float), (s) => float.Parse(s) },
        };

        private static bool HandleNullBools(string str, out bool b)
        {
            if (bool.TryParse(str, out var outp))
            {
                b = outp;
                return true;
            }
            if (str.ToLower() == "yes")
            {
                b = true;
                return true;
            }

            if (str.ToLower() == "no")
            {
                b = false;
                return true;
            }
            b = false;
            return false;
        }
    }
}