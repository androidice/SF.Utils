using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Data;
using SF.Utils.Services.DataTableServices;
using NPOI.SS.UserModel;
using System.Linq;
using NPOI.HSSF.Util;

namespace SF.Utils.Tests
{
    public class DataTableConverterTests
    {
        [Fact]
        public void ConvertDataTableToExcell() {

            IDataTableConverter dataTableConverter = new DataTableConverter();
            dataTableConverter.InitializeWorkBook();

            DataTable table = new DataTable();
            table.Columns.AddRange(new DataColumn[] {
                new DataColumn("col1"),
                new DataColumn("col2")
            });

            DataRow tempRow = table.NewRow();
            tempRow["col1"] = "Kevin Alviola";
            tempRow["col2"] = "20";
            table.Rows.Add(tempRow);

            tempRow = table.NewRow();
            tempRow["col1"] = "Chesa Alviola";
            tempRow["col2"] = "0";
            table.Rows.Add(tempRow);
            string location = @"C:\Users\kevin\Desktop\Innexus\smartfactory.test\SF.Utils.Tests\files\report files";
            dataTableConverter.ConvertDataTableToExcell(table, location, table.TableName, ApplFinancialReportStyle, ApplFinancialReportStyle, ApplyFinancialHeaderPorxy, ApplyFinancialReportRowValueProxy);
        }

        public ICellStyle ApplFinancialReportStyle(IWorkbook workbook, ISheet sheet, string columnName = "", int columnIndex = -1)
        {

            //DataTable overtimeReport = new DataTable(departmentRecords.TableName);
            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("serialNo"),
            //    new DataColumn("empName"),
            //    new DataColumn("department"),
            //    new DataColumn("weekDayOverTime"),
            //    new DataColumn("weekEndOverTime"),
            //    new DataColumn("nightShiftCount"),
            //    new DataColumn("midShiftCount"),
            //    new DataColumn("medicalLeave"),
            //    new DataColumn("noPayLeave"),
            //    new DataColumn("annualLeave"),
            //    new DataColumn("offInLiue"),
            //    new DataColumn("changeHour")
            //});

            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("workedHoursByLaw"),
            //    new DataColumn("weekdayExceedOt"),
            //    new DataColumn("weekEndExeedOt"),
            //    new DataColumn("nightShiftPay"),
            //    new DataColumn("nightShiftMealAllowance"),
            //    new DataColumn("midShiftPay"),
            //    new DataColumn("miscAllowance")
            //});

            //overtimeReport.Columns.AddRange(new DataColumn[] {
            //    new DataColumn("awayForOfficialBusiness"),
            //    new DataColumn("workInsuryLeave"),
            //    new DataColumn("remarks")
            //});
            string[] columnExceptions = new string[] { "col2" };
            bool isExists = columnExceptions.Any(identifier => identifier.Equals(columnName));        
          
            IFont font = workbook.CreateFont();
            if (isExists) 
                font.Color = IndexedColors.Red.Index;

            if (columnIndex >= 0)
                sheet.SetColumnWidth(columnIndex, 3000);

            ICellStyle cellStyle = workbook.CreateCellStyle();
            cellStyle.Alignment = HorizontalAlignment.Center;
            cellStyle.VerticalAlignment = VerticalAlignment.Center;
            cellStyle.WrapText = true;

            cellStyle.SetFont(font);

            return cellStyle;
        }

        public string ApplyFinancialHeaderPorxy(string current_value, string columnName)
        {
            //DataTable template = new DataTable(table.TableName);
            //template.Columns.AddRange(new DataColumn[] {
            //        new DataColumn("serialNo"),
            //        new DataColumn("empName"),
            //        new DataColumn("department"),
            //        new DataColumn("annualLeave"),
            //        new DataColumn("medicalLeave"),
            //        new DataColumn("noPayLeave"),
            //        new DataColumn("offInLiue"),
            //        new DataColumn("awayForOfficialBusiness"),
            //        new DataColumn("workInjuryLeave"),
            //        new DataColumn("midShiftCount"),
            //        new DataColumn("midShiftPay"),
            //        new DataColumn("nightShiftCount"),
            //        new DataColumn("nightShiftPay"),
            //        new DataColumn("nightShiftMealAllowance"),
            //        new DataColumn("miscAllowance"),
            //        new DataColumn("weekDayOverTime"),
            //        new DataColumn("weekEndOverTime"),
            //        new DataColumn("weekdayExceedOt"),
            //        new DataColumn("weekEndExeedOt"),
            //        new DataColumn("workedHoursByLaw"),
            //        new DataColumn("remarks"),
            //    });

            Dictionary<string, string> columns = new Dictionary<string, string>() {
                {"col2","序号" }   
            };

            if (columns.ContainsKey(columnName))
                return columns[columnName];

            return columnName;
        }

        public string ApplyFinancialReportRowValueProxy(string current_value, string columName)
        {
            bool isEmpty = (string.IsNullOrEmpty(current_value) || current_value.Equals("0"));
            if (isEmpty) return string.Empty;

            return current_value;
        }
    }
}
