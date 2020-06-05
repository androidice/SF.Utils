using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;

namespace SF.Utils.SFWorkBook
{
    public class SFWorkBook: ISFWorkBook
    {
        #region declerations
        private string[] accepted_formats = new string[] { ".xls", ".xlsx" };
        #endregion declerations

        #region private
        private bool IsValidFileExt(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName)) {
                string ext = fileName.Substring(fileName.LastIndexOf('.')).ToLower();
                return Array.IndexOf(accepted_formats, ext) >= 0;
            }
            return false;
        }

        private string GetNumericCellValue(ICell cell)
        {
            string cellValue = string.Empty;
            if (DateUtil.IsCellDateFormatted(cell))
            {
                DateTime date = cell.DateCellValue;

                string xya = cell.CellStyle.GetDataFormatString().ToLower();
                if (xya.Contains("y") && xya.Contains("m"))
                {
                    cellValue = date.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    cellValue = date.ToString("HH:mm:ss");
                }
            }
            else
            {
                cellValue = cell.NumericCellValue.ToString();
            }
            return cellValue;
        }

        private string GetFormulaCellValue(ICell cell)
        {
            string cellValue = string.Empty;

            if (cell.CachedFormulaResultType == CellType.Numeric)
                cellValue = cell.NumericCellValue.ToString();
            else if (cell.CachedFormulaResultType == CellType.Boolean)
                cellValue = cell.BooleanCellValue.ToString();
            else if (cell.CachedFormulaResultType == CellType.String)
                cellValue = cell.StringCellValue.ToString();
            else
                cellValue = cell.CellFormula;

            return cellValue;
        }

        #endregion private

        #region public

        /// <summary>
        /// Reads .xls and .xls format
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IWorkbook ReadWorkBook(string path)
        {
            if (IsValidFileExt(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    string ext = path.Substring(path.LastIndexOf('.')).ToLower();
                    const int XLSX_INDEX = 1;
                    if (ext.Equals(accepted_formats[XLSX_INDEX]))
                    {
                        return new XSSFWorkbook(fs);
                    }
                    else
                    {
                        return new HSSFWorkbook(fs);
                    }
                }
            }
            else
            {
                throw new Exception("The file that you are trying to read is unsupported.");
            }
        }

        public virtual string GetCellValue(ICell cell)
        {
            string cellValue = String.Empty;
            if (cell != null)
            {
                CellType cellType = cell.CellType;
                switch (cellType)
                {
                    case CellType.String:
                    case CellType.Unknown:
                        cellValue = cell.StringCellValue;
                        break;
                    case CellType.Numeric:
                        cellValue = this.GetNumericCellValue(cell);
                        break;
                    case CellType.Formula:
                        cellValue = this.GetFormulaCellValue(cell);
                        break;
                    case CellType.Boolean:
                        cellValue = cell.BooleanCellValue.ToString();
                        break;
                    case CellType.Error:
                        cellValue = cell.ErrorCellValue.ToString();
                        break;
                    default:
                        cellValue = cell.StringCellValue;
                        break;
                }
            }
            return cellValue;
        }

        #endregion public
    }
}
