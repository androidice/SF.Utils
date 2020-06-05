using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SF.Utils.Extensions
{
    public static class StringExtensions
    {
        public static string TrimAllExtraSpace(this string inputString)
        {
            Regex evaluator = new Regex(@"\s{2,}", RegexOptions.Multiline);
            return evaluator.Replace(inputString.Trim(), " ");
        }
    }
}
