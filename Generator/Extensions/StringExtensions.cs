using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Extensions
{
    public static class StringExtensions
    {
        public static string ReplaceByAlph<T>(this string instance, T alphabet) where T : IDictionary<char, char>
        {
            string buf = "";
            foreach(var ch in instance)
            {
                buf += alphabet[ch];
            }
            return buf;
        }
    }
}
