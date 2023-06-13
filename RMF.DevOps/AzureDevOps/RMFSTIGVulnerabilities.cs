using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using RMF.DevOps.Enumerations;
using RMF.DevOps.Excel;
using RMF.DevOps.Utilities;
using System.Data;

namespace RMF.DevOps.AzureDevOps
{
    public class RMFSTIGVulnerabilities : AzureDevOpsBase
    {
        private readonly WorkItem implementStep;
        private readonly IDictionary<string, WorkItem> accessControls;
        private DataTable vulnerabilities = new();
        private string workitemType = "STIG Vulnerability";

        public RMFSTIGVulnerabilities(string personalAccessToken, WorkItem implementStep, IDictionary<string, WorkItem> accessControls, string workingProject, string orgUrl) : base(personalAccessToken, workingProject, orgUrl)
        {
            this.implementStep = implementStep;
            this.accessControls = accessControls;
        }

        public async Task GenerateStigVulnerabilities(IList<RMFSTIGTypes> stigs)
        {
            var workItemTypes = await WitClient.GetWorkItemTypesAsync(workingProject);

            if (!workItemTypes.Select(x => x.Name).Contains(workitemType))
            {
                workitemType = "User Story";
            }

            if (stigs.Contains(RMFSTIGTypes.AppDevSTIG))
            {
                var stig = await FileDownloader.DownloadFileFromGitHub("AppDevStig.xlsx");

                vulnerabilities = ExcelUtility.GetExcelData(stig);
                await CreateVulnerabilities();
            }
            
            if (stigs.Contains(RMFSTIGTypes.AzureDatabaseSTIG))
            {
                var stig = await FileDownloader.DownloadFileFromGitHub("AzureDatabaseSTIG.xlsx");

                vulnerabilities = ExcelUtility.GetExcelData(stig);
                await CreateVulnerabilities();
            }

            if (stigs.Contains(RMFSTIGTypes.MSSQLServer2014Instance))
            {
                var stig = await FileDownloader.DownloadFileFromGitHub("MS_SQL_Server_2014_Instance.xlsx");

                vulnerabilities = ExcelUtility.GetExcelData(stig);
                await CreateVulnerabilities();
            }

            if (stigs.Contains(RMFSTIGTypes.MSSQLServer2014Database))
            {
                var stig = await FileDownloader.DownloadFileFromGitHub("MS_SQL_Server_2014_Database.xlsx");

                vulnerabilities = ExcelUtility.GetExcelData(stig);
                await CreateVulnerabilities();
            }

            if (stigs.Contains(RMFSTIGTypes.MSSQLServer2016Instance))
            {
                var stig = await FileDownloader.DownloadFileFromGitHub("MS_SQL_Server_2016_Instance.xlsx");

                vulnerabilities = ExcelUtility.GetExcelData(stig);
                await CreateVulnerabilities();
            }

            if (stigs.Contains(RMFSTIGTypes.MSSQLServer2016Database))
            {
                var stig = await FileDownloader.DownloadFileFromGitHub("MS_SQL_Server_2016_Database.xlsx");

                vulnerabilities = ExcelUtility.GetExcelData(stig);
                await CreateVulnerabilities();
            }

            if (stigs.Contains(RMFSTIGTypes.MSIIS10Site))
            {
                var stig = await FileDownloader.DownloadFileFromGitHub("MS_IIS_10_Site.xlsx");

                vulnerabilities = ExcelUtility.GetExcelData(stig);
                await CreateVulnerabilities();
            }

            if (stigs.Contains(RMFSTIGTypes.MSIIS10Server))
            {
                var stig = await FileDownloader.DownloadFileFromGitHub("MS_IIS_10_Server.xlsx");

                vulnerabilities = ExcelUtility.GetExcelData(stig);
                await CreateVulnerabilities();
            }
        }

        async Task CreateVulnerabilities()
        {
            foreach (var control in vulnerabilities.AsEnumerable())
            {
                var newControl = await CreateVulnerability(control);

                await GenerateRelatedControls(newControl, control);
            }
        }

        async Task<WorkItem> CreateVulnerability(DataRow? control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            var ruleTitle = control.Field<string>("Rule Title") ?? string.Empty;
            var vulnId = control.Field<string>("Vuln ID") ?? string.Empty;
            var stigId = control.Field<string>("STIG ID") ?? string.Empty;

            var patchDocument = new JsonPatchDocument
            {
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = control.Field<string>("Vuln ID") + " - " + control.Field<string>("Severity") + " - " + ruleTitle.Substring(0, Math.Min(ruleTitle.Length, 200))
                    },
                    // Add Tag
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Tags",
                        Value = "STIG Vulnerability"
                    }
            };

            if(workitemType == "STIG Vulnerability")
            {
                patchDocument.AddRange(new List<JsonPatchOperation> 
                {
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.RuleTitle",
                        Value = control.Field<string>("Rule Title")
                    },
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.Discussion",
                        Value = control.Field<string>("Discussion")
                    },
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.CheckText",
                        Value = control.Field<string>("Check Content")
                    },
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.FixText",
                        Value = control.Field<string>("Fix Text")
                    },
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.VulnId",
                        Value = vulnId.Trim()
                    },
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.Category",
                        Value = control.Field<string>("Severity")
                    },
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.STIGId",
                        Value = stigId.Trim()
                    }
                });
            }
            else
            {
                var combinedText = "<b>Discussion</b> </br>";
                combinedText += control?.Field<string>("Discussion")?.Replace("\n", "</br>") ?? string.Empty;

                combinedText += "</br></br><b>Check Content</b> </br>";
                combinedText += control?.Field<string>("Check Content")?.Replace("\n", "</br>") ?? string.Empty;

                combinedText += "</br></br><b>Fix Text</b> </br>";
                combinedText += control?.Field<string>("Fix Text")?.Replace("\n", "</br>") ?? string.Empty;

                patchDocument.Add(
                   new()
                   {
                       Operation = Operation.Add,
                       Path = "/fields/System.Description",
                       Value = combinedText
                   });
            }

            var newControl = await WitClient.CreateWorkItemAsync(patchDocument, WorkingProject, workitemType);

            await CreateParentChildRelationship(implementStep, newControl);

            return newControl;
        }

        async Task GenerateRelatedControls(WorkItem newControl, DataRow? row)
        {
            if(row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var relatedControls = row.Field<string>("NIST SP 800-53 Revision 4 References");
            
            if (string.IsNullOrWhiteSpace(relatedControls))
            {
                return;
            }

            var relatedControlList = relatedControls.Split(";").ToList();

            foreach (var relatedControl in relatedControlList)
            {
                if (string.IsNullOrWhiteSpace(relatedControl))
                {
                    continue;
                }

                var mappedControl = accessControls.FirstOrDefault(x => x.Key == relatedControl.Trim().Replace(" ", ""));

                if (mappedControl.Key == null)
                {
                    continue;
                }

                await CreateRelatedWorkItems(newControl, mappedControl.Value);
            }
        }
    }
}
