using NPOI.SS.UserModel;
using SF.Utils.Services.DataTableConverter;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SF.Utils.Services.DataTableServices
{
    public interface IDataTableConverter
    {
        void InitializeWorkBook();
        IWorkbook WorkBook { get; }

        string ConvertDataTableToExcell(DataTable table,
                                                string location,
                                                string filename = "",
                                                CreateStyle headerConfig = null,
                                                CreateStyle styleConfig = null,
                                                ValueProxy headerValueConfig = null,
                                                ValueProxy rowValueConfig = null);
    }
}
