using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using RMF.DevOps.Enumerations;

namespace RMF.DevOps.AzureDevOps
{
    public class Documents : AzureDevOpsBase
    {
        private readonly WorkItem epic;
        private string workitemType = "Documentation";

        public Documents(string personalAccessToken, WorkItem epic, string workingProject, string orgUrl) : base(personalAccessToken, workingProject, orgUrl)
        {
            this.epic = epic;
            this.workingProject = workingProject;
        }

        public async Task GenerateDocuments(IList<RMFDocumentTypes> documents)
        {
            var workItemTypes = await WitClient.GetWorkItemTypesAsync(workingProject);

            if (!workItemTypes.Select(x => x.Name).Contains(workitemType))
            {
                workitemType = "User Story";
            }

            if (documents.Contains(RMFDocumentTypes.ApplicationDevelopmentOverview))
            {
                await CreateDocument("Application Development Overview", "There are many architectural elements of a software application. This guide is to help identify application components, implementation details, and security considerations. Authentication, authorization, technologies, platforms, and interconnected systems will all be outlined. This document will also outline development processes, patterns, and systems used in the development and publication of the application.");
            }

            if (documents.Contains(RMFDocumentTypes.PersonnelSecurityProcedure))
            {
                await CreateDocument("Personnel Security Procedure", "The purpose of the DoD Personnel Security Program is to establish policies and procedures to ensure the acceptance and retention of personnel in the Armed Forces, acceptance, and retention of civilian employees in the Department of defense, and granting members of the Armed Forces, DoD civilian employees, DoD contractors, and other affiliated persons access to information are clearly consistent with the interests of national security.");
            }

            if (documents.Contains(RMFDocumentTypes.SecurityClassification))
            {
                await CreateDocument("Security Classification", "The purpose of this document  is to provide information, references, and evidence to address security classification requirements for the application.");
            }
            
            if (documents.Contains(RMFDocumentTypes.ConceptOfOperations))
            {
                await CreateDocument("Concept of Operations (CONOPS)", "The concept of operations is a document describing the characteristics of a proposed application. This document will describe these characteristics from the viewpoint of typical application users. The purpose of this document is for all stakeholders to gain an understanding of the qualitative and quantitative characteristics of the application.");
            }

            if (documents.Contains(RMFDocumentTypes.AccessControlSOP))
            {
                await CreateDocument("AC (Access Control) SOP", "Access control policies are high-level requirements that specify how access is managed and who may access information under what circumstances.");
            }

            if (documents.Contains(RMFDocumentTypes.AccessAuditAndAccountabilitySOP))
            {
                await CreateDocument("AU (Access Audit and Accountability) SOP", "Audit and accountability ensure DoD information systems are configured to audit, analyze, and report events that could harm the application.");
            }

            if (documents.Contains(RMFDocumentTypes.SecurityFocusedConfigurationManagementPlanSOP))
            {
                await CreateDocument("CM (Sec-CM Security-Focused Configuration Management Plan) SOP", "Security-Focused Configuration Management (SecCM) is the management and control of secure configurations for an information system to enable security and facilitates the management of risk. SecCM builds on the general concepts, processes, and activities of configuration management by putting attention on the implementation and maintenance of the established security requirements of the organization and information systems.");
            }

            if (documents.Contains(RMFDocumentTypes.ContingencyPlanSOP))
            {
                await CreateDocument("CP (Contingency Plan) SOP", "Information systems are vital elements in most mission/business processes. Because information system resources are so essential to an organization’s success, it is critical that identified services provided by these systems can operate effectively without excessive interruption. Contingency planning supports this requirement by establishing thorough plans, procedures, and technical measures that can enable a system to be recovered as quickly and effectively as possible following a service disruption. Contingency planning is unique to each system, providing preventive measures, recovery strategies, and technical considerations appropriate to the system’s information confidentiality, integrity, and availability requirements and the system impact level.\r\n\r\nThis document does not address facility-level information system planning (commonly referred to as a disaster recovery plan) or organizational mission continuity (commonly referred to as a continuity of operations [COOP] plan) except where it is required to restore information systems and their processing capabilities. Nor does this document address continuity of mission/business processes \r\nInformation system contingency planning refers to a coordinated strategy involving plans, procedures, and technical measures that enable the recovery of information systems, operations, and data after a disruption. Contingency planning generally includes one or more of the following approaches to restore disrupted services:\r\n\r\n1)Restoring information systems using alternate equipment.\r\n\r\n2)Performing some or all the affected business processes using alternate processing (manual) means (typically acceptable for only short-term disruptions).\r\n\r\n3)Recovering information systems operations at an alternate location (typically acceptable for only long–term disruptions or those physically impacting the facility); and\r\n\r\n4)Implementing appropriate contingency planning controls based on the information system’s security impact level.\r\n");
            }

            if (documents.Contains(RMFDocumentTypes.ArchitectureDiagram))
            {
                await CreateDocument("Architect Diagram", "The purpose of this document is to provide a high-level overview of the application architecture. This document will outline the application components, technologies, and platforms used in the development and publication of the application.");
            }

            if (documents.Contains(RMFDocumentTypes.ApplicationConfigurationGuide))
            {
                await CreateDocument("Application Configuration Guide", "The Application Configuration Guide is any document or collection of documents used to configure the application. These documents may be part of a user guide, secure configuration guide, or any guidance that satisfies the requirements provided herein. Configuration examples include but are not limited to: - Encryption Settings - PKI Certificate Configuration Settings - Password Settings - Auditing configuration - AD configuration - Backup and disaster recovery settings - List of hosting enclaves and network connection requirements - Deployment configuration settings - Known security assumptions, implications, system level protections, best practices, and required permissions Development systems, build systems, and test systems must operate in a standardized environment. These settings are to be documented in the Application Configuration Guide. Examples include but are not limited to: - List of development systems, build systems, and test systems. - Versions of compilers used - Build options when creating applications and components - Versions of COTS software (used as part of the application) - Operating systems and versions - For web applications, which browsers and what versions are supported. All deployment configuration settings are to be documented in the Application Configuration Guide and the Application Configuration Guide must be made available to application hosting providers and application/system administrators.");
            }

            if (documents.Contains(RMFDocumentTypes.ThreatModel))
            {
                await CreateDocument("Threat Model", "A threat model document is a comprehensive document that describes the potential threats and vulnerabilities of a system or application. It helps identify and understand potential risks to the system's security and provides recommendations for mitigating those risks. ");
            }
        }

        private async Task CreateDocument(string title, string description)
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

                // RMF Description
                new()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Description",
                    Value = description.Replace("\n", "</br>")
                },
                // Add Tag
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Tags",
                    Value = "Documentation"
                }
            };

            var documentation = await WitClient.CreateWorkItemAsync(patchDocument, workingProject, workitemType);

            if (documentation == null)
            {
                throw new Exception("Can't create documentation");
            }

            await CreateParentChildRelationship(epic, documentation);
        }
    }
}
