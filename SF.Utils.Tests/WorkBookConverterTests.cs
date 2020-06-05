using System;
using System.Data;
using Xunit;
using SF.Utils.WorkBookConverter;

namespace SF.Utils.Tests
{
    public class WorkBookConverterTests
    {
        /// <summary>
        /// Test the WorkBookConverter if can successfully convert .xlsx file to the data table
        /// with zero configuration
        /// </summary>
        [Fact]
        public void CONVERT_XLSX_TO_DATA_TABLE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            /**
             * Convert the whole .xlsx file to data table
             */
            string fileLocation = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLSX_50.xlsx";
            var result = workBookConverter.ConvertWorkBookToDataTable(fileLocation);
            Assert.IsType<DataTable>(result);

        }


        [Fact]
        public void CONVERT_TO_DATA_TABLE_SHOULD_SUPPORT_TABLE_NAME()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            /**
             * Convert the whole .xlsx file to data table
             */
            string fileLocation = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLSX_50.xlsx";
            var result = workBookConverter.ConvertWorkBookToDataTable(fileLocation);
            Assert.Equal("file_example_XLSX_50", result.TableName);
        }


        /// <summary>
        /// Test the WorkBookConverter if can successfully convert .xls file to the datatable
        /// with zero configuration
        /// </summary>
        [Fact]
        public void CONVERT_XLS_TO_DATA_TABLE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();
            /**
             * Convert the whole .xls file to data table
             */
            string fileLocation = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLS_50.xls";
            var result = workBookConverter.ConvertWorkBookToDataTable(fileLocation);
            Assert.IsType<DataTable>(result);
            Assert.Equal(51, result.Rows.Count);//current test file contains 51 rows
        }
        /// <summary>
        /// Test the WorkBookConverter if can successfully convert .xlsx file to datatable
        /// with row and col configuarion to start read the workbook
        /// </summary>
        [Fact]
        public void CONVERT_XLSX_TO_DATA_TABLE_WITH_START_ROW_AND_COL_CONFIGURATION()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            string fileLocation = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLSX_50_1.xlsx";
            var result = workBookConverter.ConvertWorkBookToDataTable(fileLocation, 6, 2);
            Assert.IsType<DataTable>(result);
            Assert.Equal(51, result.Rows.Count);
        }


        /// <summary>
        /// Test the WorkBookConverter if can successfully convert .xls file to datatable
        /// with row and col configuarion to start read the workbook
        /// </summary>
        [Fact]
        public void CONVERT_XLS_TO_DATATA_TABLE_WITH_START_ROW_AND_CON_CONFIGURATION()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            string fileLocation = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\file_example_XLS_50_1.xls";
            var result = workBookConverter.ConvertWorkBookToDataTable(fileLocation, 3, 1);
            Assert.IsType<DataTable>(result);
            Assert.Equal(51, result.Rows.Count);
        }
    }
}
