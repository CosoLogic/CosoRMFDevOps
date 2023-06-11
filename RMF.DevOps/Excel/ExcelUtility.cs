using ClosedXML.Excel;
using System.Data;

namespace RMF.DevOps.Excel
{
    public static class ExcelUtility
    {
        public static DataTable GetExcelData(string filePath)
        {
            var dt = new DataTable();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed();

            // Add columns to DataTable
            foreach (var cell in rows.First().CellsUsed())
            {
                dt.Columns.Add(cell.Value.ToString());
            }

            // Add rows to DataTable
            foreach (var row in rows.Skip(1))
            {
                var dataRow = dt.NewRow();
                foreach (var cell in row.CellsUsed())
                {
                    dataRow[cell.WorksheetColumn().ColumnNumber() - 1] = cell.Value.ToString();
                }
                dt.Rows.Add(dataRow);
            }

            return dt;
        }
    }
}
