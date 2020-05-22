using System;
using System.Data;
using Xunit;
using SF.Utils.WorkBookConverter;

namespace SF.Utils.Tests
{
    public class WorkBookConverterTests
    {
        /// <summary>
        /// Test the WorkBookConverter if can successfully convert .xlsx and .xls file
        /// </summary>
        [Fact]
        public void CONVERT_XLS_AND_XLSX_TO_DATA_TABLE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            /**
             * Convert the whole .xlsx file to data table
             */
            var result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLSX_50.xlsx");
            Assert.IsType<DataTable>(result);
            /**
             * Convert the whole .xls file to data table
             */
            result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLS_50.xls");
            Assert.IsType<DataTable>(result);
            Assert.Equal(51, result.Rows.Count);//current test file contains 51 rows
        }

        /// <summary>
        /// Test the WorkBookConverter if can successfully convert .xlsx and .xls file
        /// with row and col to start red the workbook
        /// </summary>
        [Fact]
        public void CONVERT_XLS_AND_XLSX_TO_DATA_TABLE_WITH_START_ROW_AND_COL_CONFIGURABLE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            var result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLSX_50_1.xlsx", 6,2);
            Assert.IsType<DataTable>(result);
            Assert.Equal(51, result.Rows.Count);
        }
    }
}
