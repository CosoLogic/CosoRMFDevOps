using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using RMF.DevOps.Enumerations;
using RMF.DevOps.Excel;
using System.Data;

namespace RMF.DevOps.AzureDevOps
{
    public class RMFSteps : AzureDevOpsBase
    {
        private readonly WorkItem epic;
        private readonly List<string> applicableControls;
        private readonly string orgUrl;
        private DataTable excelSteps = new();
        private string workitemType = "RMF Step";

        public RMFSteps(string personalAccessToken, WorkItem epic, string workingProject, List<string> applicableControls, string orgUrl) : base(personalAccessToken, workingProject, orgUrl)
        {
            this.epic = epic;
            this.applicableControls = applicableControls;
            this.orgUrl = orgUrl;
            this.applicableControls = applicableControls;
            this.epic = epic;
        }

        public async Task GenerateRMFSteps(List<RMFSTIGTypes> stigs)
        {
            var workItemTypes = await WitClient.GetWorkItemTypesAsync(workingProject);

            if (!workItemTypes.Select(x => x.Name).Contains(workitemType))
            {
                workitemType = "User Story";
            }

            var stepsDocumentPath = "RMF_Steps.xlsx";
            var stepsDocumentFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SourceDocs", stepsDocumentPath);

            excelSteps = ExcelUtility.GetExcelData(stepsDocumentFullPath);

            await CreateStep("Prepare");
            await CreateStep("Categorize");
            await CreateStep("Select");
            var implementStep = await CreateStep("Implement");
            await CreateStep("Assess");
            await CreateStep("Authorize");
            await CreateStep("Monitor");

            await new RMFAccessControls(base.personalAccessToken, implementStep, workingProject, applicableControls, orgUrl).GenerateAccessControls(stigs);
        }

        private async Task<WorkItem> CreateStep(string stepName)
        {
            var row = excelSteps.AsEnumerable().FirstOrDefault(row => row.Field<string>("Title") == stepName);

            if (row == null)
            {
                throw new Exception($"{stepName} step not found in RMF_Steps.xlsx");
            }

            var description = row.Field<string>("Description");
            var source = row.Field<string>("Source");

            if (description == null || source == null)
            {
                throw new Exception($"{stepName} step in RMF_Steps.xlsx is missing Description or Source");
            }

            return await CreateRMFStepWorkItem(stepName, description, source);
        }

        async Task<WorkItem> CreateRMFStepWorkItem(string title, string description, string source)
        {
            // Define the new epic to create
            var patchDocument = new JsonPatchDocument
            {
                new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = title
                },
                // Add Tag
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Tags",
                    Value = "RMF Step"
                }
            };

            if (workitemType == "RMF Step")
            {
                patchDocument.Add(new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Description",
                    Value = description.Replace("\n", "</br>")
                });

                patchDocument.Add(
                    new()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/Custom.Sources",
                        Value = $"<a href='{source}'>{source}</a>"
                    });
            }
            else
            {
                var combinedText = "<b>Description</b> </br>";
                combinedText += !string.IsNullOrWhiteSpace(source) ? source.Replace("\n", "</br>") : string.Empty;
                combinedText += "</br></br>";

                combinedText += "<b>Sources</b> </br>";
                combinedText += !string.IsNullOrWhiteSpace(source) ? "</br></br>" + source.Replace("\n", "</br>") : string.Empty;

                patchDocument.Add(
                   new()
                   {
                       Operation = Operation.Add,
                       Path = "/fields/System.Description",
                       Value = combinedText
                   });
            }

            // Create the new rmf step
            var rmfStep = await WitClient.CreateWorkItemAsync(patchDocument, WorkingProject, workitemType);

            if (rmfStep == null)
            {
                throw new Exception("Can't create RMF step");
            }

            await CreateParentChildRelationship(epic, rmfStep);

            return rmfStep;
        }
    }
}
