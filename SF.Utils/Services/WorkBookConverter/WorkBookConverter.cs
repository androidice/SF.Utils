using NPOI.SS.UserModel;
using SF.Utils.SFWorkBook;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SF.Utils.WorkBookConverter
{
    public class WorkBookConverter: IWorkBookConverter
    {
        private readonly ISFWorkBook wbService = new SFWorkBook.SFWorkBook();

        public virtual DataTable ConvertWorkBookToDataTable(string path, int beginRow = 0, int beginCol = 0)
        {
            IWorkbook wb = wbService.ReadWorkBook(path); // read the workbook
            int noOfSheets = wb.NumberOfSheets; // get the number of sheets;
            DataTable result = new DataTable();

            InitializeHeaders(result, beginRow, beginCol, wb);// initialize DataTable headers from excell header

            for (var index = 0; index < noOfSheets; index++)
            {
                ISheet sheet = wb.GetSheetAt(index);
                int noOfRows = sheet.LastRowNum;
                noOfRows = noOfRows + beginRow ;
                for (var rowIndex = beginRow; rowIndex <= noOfRows; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row != null) {
                        int noOfCols = row.Cells.Count;
                        noOfCols = (noOfCols + beginCol);
                        DataRow tempRow = result.NewRow();

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
                    }
                }
            }

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
