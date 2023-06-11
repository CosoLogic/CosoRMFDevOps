using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using RMF.DevOps.Enumerations;
using RMF.DevOps.Excel;
using System.Data;

namespace RMF.DevOps.AzureDevOps
{
    public class RMFAccessControls : AzureDevOpsBase
    {
        private readonly WorkItem implementStep;
        private readonly List<string> applicableControls;
        private readonly string orgUrl;
        private DataTable excelControls = new();
        private string workitemType = "Control";

        public RMFAccessControls(string personalAccessToken, WorkItem implementStep, string workingProject, List<string> applicableControls, string orgUrl) : base(personalAccessToken, workingProject, orgUrl)
        {
            this.implementStep = implementStep;
            this.applicableControls = applicableControls;
            this.orgUrl = orgUrl;
        }

        public async Task GenerateAccessControls(List<RMFSTIGTypes> stigs)
        {
            const string controlsDocumentPath = "NIST_SP-800-53_rev5.xlsx";
            var controlsDocumentFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SourceDocs", controlsDocumentPath);

            excelControls = ExcelUtility.GetExcelData(controlsDocumentFullPath);

            var workItemsCreated = await CreateAccessControls(implementStep);

            await new RMFSTIGVulnerabilities(base.personalAccessToken, implementStep, workItemsCreated, workingProject, orgUrl).GenerateStigVulnerabilities(stigs);
        }

        async Task<Dictionary<string, WorkItem>> CreateAccessControls(WorkItem implementStep)
        {
            var workItemTypes = await WitClient.GetWorkItemTypesAsync(workingProject);

            if (!workItemTypes.Select(x => x.Name).Contains(workitemType))
            {
                workitemType = "User Story";
            }

            var workItemsCreated = new Dictionary<string, WorkItem>();

            foreach(var control in excelControls.AsEnumerable())
            {
                // Only create controls that apply to the impact level of the project
                var controlIdentifier = control.Field<string>("Control Identifier");
                if (controlIdentifier != null && !applicableControls.Contains(controlIdentifier))
                {
                    continue;
                }

                var newControl = await CreateAccessControl(control);

                await CreateParentChildRelationship(implementStep, newControl);

                if (controlIdentifier == null) continue;
                
                workItemsCreated.Add(controlIdentifier.Trim(), newControl);

                var relatedControlsFull = control.Field<string>("Related Controls");

                if (relatedControlsFull == null) continue;
                    
                foreach (var subControl in relatedControlsFull.Split(",").ToList())
                {
                    var existingId = workItemsCreated.FirstOrDefault(x => x.Key == subControl);

                    if (existingId is not { Key: not null, Value.Id: not null }) continue;
                    var existingControl = await WitClient.GetWorkItemAsync(existingId.Value.Id.Value);
                    await CreateRelatedWorkItems(newControl, existingControl);
                }
            }

            return workItemsCreated;
        }

        private async Task<WorkItem> CreateAccessControl(DataRow? control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            var controlText = control.Field<string>("Control");
            var dicussionText = control.Field<string>("Discussion");

            var patchDocument = new JsonPatchDocument
                {
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = control.Field<string>("Control Identifier") + " - " + control.Field<string>("Control Name")
                    },
                    // Add Tag
                    new JsonPatchOperation
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Tags",
                        Value = "Control"
                    }
                };

            if (workitemType == "Control")
            {
                patchDocument.Add(
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.Discussion",
                        Value = !string.IsNullOrWhiteSpace(dicussionText) ? dicussionText.Replace("\n", "</br>") : string.Empty
                    });

                patchDocument.Add(
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Description",
                        Value = !string.IsNullOrWhiteSpace(controlText) ? controlText.Replace("\n", "</br>") : string.Empty
                    });
            }
            else
            {
                var combinedText = "<b>Description</b> </br>";
                combinedText += !string.IsNullOrWhiteSpace(controlText) ? controlText.Replace("\n", "</br>") : string.Empty;
                combinedText += "</br></br>";
                combinedText += "<b>Discussion</b>";
                combinedText += !string.IsNullOrWhiteSpace(dicussionText) ? "</br></br>" + dicussionText.Replace("\n", "</br>") : string.Empty;

                patchDocument.Add(
                   new()
                   {
                       Operation = Operation.Add,
                       Path = "/fields/System.Description",
                       Value = combinedText
                   });
            }

            var newControl = await WitClient.CreateWorkItemAsync(patchDocument, WorkingProject, workitemType);

            if (newControl == null)
            {
                throw new Exception("Can't create control");
            }

            return newControl;
        }
    }
}
