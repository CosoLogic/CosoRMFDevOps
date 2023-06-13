using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace RMF.DevOps.AzureDevOps
{
    public class Epic : AzureDevOpsBase
    {
        private readonly string impactLevel;

        public Epic(string personalAccessToken, string impactLevel, string workingProject, string orgUrl): base(personalAccessToken, workingProject, orgUrl)
        {
            this.impactLevel = impactLevel;
        }

        public async Task<WorkItem> CreateEpic(string title, string? description)
        {
            // Define the new epic to create
            var patchDocument = new JsonPatchDocument
            {
                new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = title + ": Impact Level " + impactLevel
                },

                new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Description",
                    Value = description
                }
            };

            // Create the new epic
            var epic = await WitClient.CreateWorkItemAsync(patchDocument, base.workingProject, "Epic");

            if (epic is { Id: not null })
            {
                Console.WriteLine($"Epic {epic.Id} created successfully.");
            }
            else
            {
                throw new Exception("Failed to create epic");
            }

            return epic;
        }

        public async Task DeleteEpic(WorkItem epic)
        {
            if (epic.Id == null)
            {
                return;
            }

            await DeleteWorkItemRecursive(epic.Id.Value);
        }

        public async Task DeleteWorkItemRecursive(int workItemId)
        {
            // Get the work item details
            var workItem = await WitClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations);

            // Delete all child work items recursively
            foreach (var childLink in workItem.Relations.Where(r => r.Rel == "System.LinkTypes.Hierarchy-Forward"))
            {
                var childId = int.Parse(childLink.Url.Substring(childLink.Url.LastIndexOf('/') + 1));
                await DeleteWorkItemRecursive(childId);
            }

            // Delete the work item
            await WitClient.DeleteWorkItemAsync(workItemId);
        }
    }
}
