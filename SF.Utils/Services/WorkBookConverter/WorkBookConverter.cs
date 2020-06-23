using NPOI.SS.UserModel;
using SF.Utils.SFWorkBook;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.IO;
using SF.Utils.Services.Logger;
using Microsoft.Extensions.Logging;

namespace SF.Utils.WorkBookConverter
{
    public class WorkBookConverter: IWorkBookConverter
    {
        private readonly ISFWorkBook wbService = new SFWorkBook.SFWorkBook();
        private readonly ILoggerService loggerService;
        private readonly ILogger logger;

        public WorkBookConverter() {
            this.loggerService = new LoggerService();
            this.logger = loggerService.CreateLogger(GetType().FullName);
        }

        public WorkBookConverter(ILogger logger) {
            this.logger = logger;
        }

        public virtual DataTable ConvertWorkBookToDataTable(string path, int beginRow = 0, int beginCol = 0)
        {
            IWorkbook wb = wbService.ReadWorkBook(path); // read the workbook
            int noOfSheets = wb.NumberOfSheets; // get the number of sheets;
            string fileName = Path.GetFileNameWithoutExtension(path);
            DataTable result = new DataTable(fileName);// initialize the datatable with file name for tracing

            this.logger.LogInformation(string.Format("Converting {0} to virtual table", fileName));
            InitializeHeaders(result, beginRow, beginCol, wb);// initialize DataTable headers from excell header

            for (var index = 0; index < noOfSheets; index++)
            {
                ISheet sheet = wb.GetSheetAt(index);
                int noOfRows = sheet.LastRowNum;
                noOfRows = noOfRows + beginRow ;
                decimal totalRecords = noOfRows;
                decimal transfer = 0;
                decimal percentage = 0;

                for (var rowIndex = beginRow; rowIndex <= noOfRows; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row != null) {
                        int noOfCols = row.Cells.Count;
                        noOfCols = (noOfCols + beginCol);
                        DataRow tempRow = result.NewRow();

                        percentage = (transfer / totalRecords) * 100;
                        this.logger.LogInformation(string.Format("Converting {0} to virtual table [{1} - {2}] ({3}%)", fileName, transfer, totalRecords, percentage.ToString("0#")));

                        int tableColumnIndex = 0;
                        for (var colIndex = beginCol; colIndex < noOfCols; colIndex++)
                        {
                            ICell cell = row.GetCell(colIndex);
                            if (cell != null) {
                                bool isColExists = result.Columns.Contains(tableColumnIndex.ToString());
                                if (isColExists) {
                                    string cellValue = string.Empty;
                                    string colName = result.Columns[tableColumnIndex].ToString(); // get data column name from datatable

                                    cellValue = wbService.GetCellValue(cell); // get the cell value

                                    tempRow[colName] = cellValue;
                                    tableColumnIndex++;
                                }
                            }
                        }

                        result.Rows.Add(tempRow);
                        transfer++;
                    }
                }
            }
            this.logger.LogInformation(string.Format("Converting {0} to virtual table has been completed", fileName));
            return result;
        }

        public virtual void InitializeHeaders(DataTable dt, int beginRow, int beginCol, IWorkbook wb)
        {
            ISheet headerSheet = wb.GetSheetAt(0);
            IRow headerRow = headerSheet.GetRow(beginRow);
            int noOfCols = headerRow.Cells.Count;
            noOfCols = (noOfCols + beginCol);

            var columnIndex = 0;

            for (var colIndex = beginCol; colIndex < noOfCols; colIndex++)
            {
                ICell cell = headerRow.GetCell(colIndex);
                if (cell != null)
                {
                    string colName = columnIndex.ToString();
                    dt.Columns.Add(new DataColumn()
                    {
                        ColumnName = colName
                    });
                    columnIndex++;
                }
            }
        }
    }
}
