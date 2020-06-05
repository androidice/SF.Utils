using System;
using System.Collections.Generic;
using System.Text;

namespace SF.Utils.Extensions
{
    public static class DateExtensions
    {
        public static bool IsWeekEnd(this DateTime date) =>
            (date.DayOfWeek.Equals(DayOfWeek.Saturday) || date.DayOfWeek.Equals(DayOfWeek.Sunday));

    }
}
