using RMF.DevOps.Excel;
using System.Data;

namespace RMF.DevOps.AzureDevOps
{
    public class RMFControlSelection
    {
        public Tuple<List<string>, string> GetControlSelection(string impactLevel)
        {
            string controlsDocumentPath = "sp800-53b-control-baselines.xlsx";
            string controlsDocumentFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SourceDocs", controlsDocumentPath);

            var excelControls = ExcelUtility.GetExcelData(controlsDocumentFullPath);

            var applicableControls = new List<string>();
            foreach (var control in excelControls.AsEnumerable())
            {
                if (control.Field<string>("Security Control Baseline - " + impactLevel) != "x") continue;

                var controlIdentifier = control.Field<string>("Control Identifier");

                if (controlIdentifier != null)
                {
                    applicableControls.Add(controlIdentifier);
                }
            }

            return new Tuple<List<string>, string>(applicableControls, impactLevel);
        }
    }
}
