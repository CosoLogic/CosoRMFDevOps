using RMF.DevOps.Excel;
using RMF.DevOps.Utilities;
using System.Data;

namespace RMF.DevOps.AzureDevOps
{
    public class RMFControlSelection
    {
        public async Task<Tuple<List<string>, string>> GetControlSelection(string impactLevel)
        {
            if (impactLevel.ToLower() == "skip")
            {
                return new Tuple<List<string>, string>(new List<string>(), impactLevel);
            }

            string controlsDocumentPath = await FileDownloader.DownloadFileFromGitHub("sp800-53b-control-baselines.xlsx");

            var excelControls = ExcelUtility.GetExcelData(controlsDocumentPath);

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
