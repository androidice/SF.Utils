using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using SF.Utils.Services.DataTableConverter;
using SF.Utils.Services.Logger;
using Microsoft.Extensions.Logging;

namespace SF.Utils.Services.DataTableServices
{
    public class DataTableConverter : IDataTableConverter
    {
        public IWorkbook WorkBook { get; private set; }
        private readonly ILoggerService loggerService;
        private readonly ILogger logger;

        public DataTableConverter()
        {
            this.loggerService = new LoggerService();
            this.logger = loggerService.CreateLogger(GetType().FullName);
        }

        public DataTableConverter(ILogger logger)
        {
            this.logger = logger;
        }

        public void InitializeWorkBook() {
            this.WorkBook = new XSSFWorkbook();
        } 

        public string ConvertDataTableToExcell(DataTable table, 
                                                string location, 
                                                string filename = "", 
                                                CreateStyle headerConfig = null, 
                                                CreateStyle styleConfig = null,
                                                ValueProxy headerValueConfig = null,
                                                ValueProxy rowValueConfig = null)
        {
            string _filename = string.IsNullOrEmpty(filename)? DateTime.Now.ToString("yyyyMMddHHmmss"): filename;
            _filename = string.Concat(_filename, ".xlsx");
            location = Path.Combine(location, _filename);

            this.logger.LogInformation("Generating excell file for {0}", Path.GetFileNameWithoutExtension(location));

            using (FileStream fs = new FileStream(location, FileMode.Create, FileAccess.Write))
            {
                ISheet sheet = WorkBook.CreateSheet("Sheet1");

                int hColIndex = 0;
                IRow hrow = sheet.CreateRow(0);
                foreach (DataColumn column in table.Columns)
                {
                    ICell headerCell =  hrow.CreateCell(hColIndex);
                    string columnName = column.ColumnName;
                    if (headerValueConfig != null) columnName = headerValueConfig.Invoke(columnName, columnName);

                    headerCell.SetCellValue(columnName);
                    headerCell.CellStyle = headerConfig?.Invoke(this.WorkBook, sheet, column.ColumnName, hColIndex);
                    hColIndex++;
                }

                int rowIndex = 1;
                decimal totalRecords = table.Rows.Count;
                decimal transfer = 0;
                decimal percentage = 0;

                foreach (DataRow dataRow in table.Rows)
                {
                    IRow sheetRow = sheet.CreateRow(rowIndex);
                    int colIndex = 0;

                    percentage = (transfer / totalRecords) * 100;
                    this.logger.LogInformation("Generating excell file for {0} [{1} - {2}] ({3}%)", Path.GetFileNameWithoutExtension(location), transfer, totalRecords, percentage.ToString("0#"));

                    foreach (DataColumn dataCol in table.Columns)
                    {
                        ICell cell = sheetRow.CreateCell(colIndex);
                        string value = dataRow[dataCol.ColumnName].ToString();
                        if (rowValueConfig != null) value = rowValueConfig.Invoke(value, dataCol.ColumnName);

                        cell.SetCellValue(value);
                        cell.CellStyle = styleConfig?.Invoke(this.WorkBook, sheet, dataCol.ColumnName);
                        colIndex++;
                    }
                    rowIndex++;
                    transfer++;
                }
                this.WorkBook.Write(fs);
                this.logger.LogInformation("Generating excell file for {0} [{1} - {2}] ({3}%)", Path.GetFileNameWithoutExtension(location), transfer, totalRecords, percentage.ToString("0#"));
            }

            return location;
        }
    }
}
