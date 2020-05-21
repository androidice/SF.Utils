using SF.Utils.WorkBookConverter;
using SF.Utils.Validators;
using System;
using System.Data;
using Xunit;
using System.Collections.Generic;

namespace SF.Utils.Tests
{
    public class AttendanceManagementTests
    {
        [Fact]
        public void CONVERT_GUARD_ROOM_FILE_TO_DATA_TABLE()
        {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();

            var result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\guard room file.xls", 0, 1);
            Assert.IsType<DataTable>(result);
        }


        /// <summary>
        /// Validate if the guard room file is valid
        /// </summary>
        [Fact]
        public void VALIDATE_GUARD_ROOM_FILE() {
            IWorkBookConverter workBookConverter = new WorkBookConverter.WorkBookConverter();
            const int TIME_STAMP_INSTANCE = 5;
            var result = workBookConverter.ConvertWorkBookToDataTable(@"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\workbook files\guard room file.xls", 0, 1);
            bool isValid = result.ValidateColumn(new List<DataColumnValidatorModel>() {
                new DataColumnValidatorModel(){
                    columnName = TIME_STAMP_INSTANCE.ToString(),
                    expectedType = typeof(DateTime),
                    pattern = "yyyy-MM-dd HH:mm:ss"
                }
            });

            Assert.True(isValid);// if false handle error message on the caller
        }
    }
}
