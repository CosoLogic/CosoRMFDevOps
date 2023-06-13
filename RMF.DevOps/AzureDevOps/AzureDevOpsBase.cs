using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace RMF.DevOps.AzureDevOps
{
    public class AzureDevOpsBase
    {

        protected string workingProject = string.Empty;
        protected readonly string personalAccessToken;
        private readonly string orgUrl;

        protected AzureDevOpsBase(string personalAccessToken, string workingProject, string orgUrl)
        {
            this.personalAccessToken = personalAccessToken;
            WorkingProject = workingProject;
            this.orgUrl = orgUrl;
        }

        private WorkItemTrackingHttpClient GetClient()
        {
            var connection = new VssConnection(new Uri(orgUrl), new VssBasicCredential(string.Empty, personalAccessToken));
            return connection.GetClient<WorkItemTrackingHttpClient>();
        }

        protected WorkItemTrackingHttpClient WitClient => GetClient();

        public string WorkingProject
        {
            get => workingProject;
            protected set => workingProject = value;
        }

        protected async Task CreateRelatedWorkItems(WorkItem item1, WorkItem item2)
        {
            if (item1 == null)
            {
                throw new ArgumentNullException(nameof(item1));
            }

            if (item2 == null)
            {
                throw new ArgumentNullException(nameof(item2));
            }

            if (!item2.Id.HasValue || !item1.Id.HasValue)
            {
                return;
            }

            var patchDocument = new JsonPatchDocument
            {
                new()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new WorkItemRelation()
                    {
                        Rel = "System.LinkTypes.Related",
                        Url = $"{orgUrl}/_apis/wit/workItems/{item2.Id.Value}",
                        Attributes = new Dictionary<string, object>() {
                        { "comment", "Linked" }
                    }
                    }
                }
            };

            // Update the epic work item with the new parent-child relationship
            await WitClient.UpdateWorkItemAsync(patchDocument, item1.Id.Value);
        }

        protected async Task CreateParentChildRelationship(WorkItem parent, WorkItem child)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            if (!child.Id.HasValue || !parent.Id.HasValue)
            {
                return;
            }

            var patchDocument = new JsonPatchDocument
            {
                new()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new WorkItemRelation()
                    {
                        Rel = "System.LinkTypes.Hierarchy-Forward",
                        Url = $"{orgUrl}/_apis/wit/workItems/{child.Id.Value}",
                        Attributes = new Dictionary<string, object>() {
                        { "comment", "Linked to child" }
                    }
                    }
                }
            };

            // Update the epic work item with the new parent-child relationship
            await WitClient.UpdateWorkItemAsync(patchDocument, parent.Id.Value);
        }
    }
}
