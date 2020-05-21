using System;
using System.Data;
using Xunit;
using SF.Utils.WorkBookConverter;

namespace SF.Utils.Tests
{
    public class WorkBookTests
    {
        /// <summary>
        /// Test the WorkBookConverter if can successfully convert .xlsx and .xls file
        /// </summary>
        [Fact]
        public void Should_Convert_XLS_AND_XLSX_TO_DATA_TABLE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            /**
             * Convert the whole .xlsx file to data table
             */
            DataTable dt = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLSX_50.xlsx");


            /**
             * Convert the whole .xls file to data table
             */
            dt = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLS_50.xls");
        }
    }
}
