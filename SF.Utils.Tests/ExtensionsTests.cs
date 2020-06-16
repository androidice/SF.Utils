using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using SF.Utils.Extensions;

namespace SF.Utils.Tests
{
    public class DateExtensionsTests
    {
        /// <summary>
        /// Check date if weekend
        /// </summary>
        [Fact]
        public void CHECK_DATE_IS_WEEKEND()
        {
            DateTime date = new DateTime(2020, 6, 6);// saturday
            DateTime date2 = new DateTime(2020, 6, 7);// sunday

            Assert.True(date.IsWeekEnd());
            Assert.True(date2.IsWeekEnd());
        }

        /// <summary>
        /// Check date if weekday
        /// </summary>
        [Fact]
        public void CHECK_DATE_IS_WEEKDAY()
        {
            DateTime date = new DateTime(2020, 6, 3);// can be any date on weekday
            Assert.True(!date.IsWeekEnd());
        }

    }

    public class StringExtensionsTests
    {

        [Fact]
        public void TRIM_ALL_SPACE()
        {
            string input = " remaining essentially   unchanged.  ";
            input = input.TrimAllExtraSpace();
            Assert.Equal("remaining essentially unchanged.", input);
        }
    }

    public class DecimalExtensions
    {

        [Fact]
        public void CONVERT_TO_CURRENCY()
        {
            decimal input = 12345.67m;
            Assert.Equal("$12,345.67", input.FormatToCurrency());
        }

        [Fact]
        public void CONVERT_TO_CURRENCY_WITH_SUPPORTED_CULTURE()
        {
            decimal input = 12345.67m;
            Assert.Equal("¥12,345.67", input.FormatToCurrency(new System.Globalization.CultureInfo("zh-CN")));
        }

        [Fact]
        public void CONVERT_TO_NUMBER_FORMAT()
        {
            decimal input = 12345.00m;
            Assert.Equal("12,345", input.ConvertToNumericFormat());
        }
    }
}
