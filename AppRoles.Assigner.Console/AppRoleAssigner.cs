using Microsoft.Graph;
using Microsoft.Graph.Models;
using Spectre.Console;

namespace AppRoles.Assigner.Console;

public sealed class AppRoleAssigner(GraphServiceClient graphClient)
{
    public async Task AssignAsync(CancellationToken token = default)
    {
        var applications = await GetApplicationsAsync(token);
        var applicationsWithAppRole = applications.Where(x => x.AppRoles.Any());

        var filteredApplications = FilterWithFuzzySearch(applicationsWithAppRole.Select(x => x.DisplayName).ToArray(), "application containing the app role");

        AnsiConsole.Clear();

        var selectedApplicationName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select the application containing the app role: ")
                .AddChoices(filteredApplications));

        AnsiConsole.Clear();

        var applicationServicePrincipal = await GetServicePrincipalAsync(selectedApplicationName, token);

        AnsiConsole.Clear();

        var selectedServicePrincipalType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select the type of application to assign the app role to: ")
                .AddChoices(["Application", "ManagedIdentity"]));

        AnsiConsole.Clear();

        var servicePrincipals = await GetServicePrincipals(selectedServicePrincipalType, token);

        AnsiConsole.Clear();

        var filteredServicePrincipals = FilterWithFuzzySearch(servicePrincipals.Select(x => x.DisplayName).ToArray(), $"{selectedServicePrincipalType} that needs the app role permission to '{applicationServicePrincipal.DisplayName}'");

        AnsiConsole.Clear();

        var selectedManagedIdentityName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Select the {selectedServicePrincipalType} to assign the app role to: ")
                .AddChoices(filteredServicePrincipals));

        
        AnsiConsole.Clear();

        var servicePrincipalWhichWillGetPermission = await GetServicePrincipalAsync(selectedManagedIdentityName, token);

        AnsiConsole.Clear();

        var selectedAppRole = AnsiConsole.Prompt(
            new SelectionPrompt<AppRole>()
                .UseConverter(x => x.DisplayName)
                .Title("Select the app role to assign: ")
                .AddChoices(applicationServicePrincipal.AppRoles));

        var requestBody = new AppRoleAssignment
        {
            PrincipalId = Guid.Parse(servicePrincipalWhichWillGetPermission.Id),
            ResourceId = Guid.Parse(applicationServicePrincipal.Id),
            AppRoleId = selectedAppRole.Id,
        };

        AnsiConsole.Clear();

        var chosen =
            await AnsiConsole.ConfirmAsync(
                $"Do you want to give [springgreen2]{servicePrincipalWhichWillGetPermission.DisplayName}[/] {selectedServicePrincipalType} [orange1]{selectedAppRole.DisplayName}[/] permissions to [deepskyblue1]{applicationServicePrincipal.DisplayName}[/]?",
                false, token);

        if (chosen)
        {
            await graphClient.ServicePrincipals[servicePrincipalWhichWillGetPermission.Id].AppRoleAssignedTo
                .PostAsync(requestBody, cancellationToken: token);
        }
    }

    private async Task<List<ServicePrincipal>> GetServicePrincipals(string applicationType, CancellationToken token = default)
    {
        var servicePrincipals = await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            var progress = ctx.AddTask($"Fetching {applicationType} service principals");

            var getServicePrincipalsResponse = await graphClient.ServicePrincipals.GetAsync(
                (requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = $"servicePrincipalType eq '{applicationType}'";
                }, token);

            var servicePrincipals = new List<ServicePrincipal>();

            var pageIterator = PageIterator<ServicePrincipal, ServicePrincipalCollectionResponse>.CreatePageIterator(
                graphClient,
                getServicePrincipalsResponse,
                (sp) =>
                {
                    progress.Increment(0.5);
                    servicePrincipals.Add(sp);
                    return Task.FromResult(true);
                });

            await pageIterator.IterateAsync(token);

            while (pageIterator.State != PagingState.Complete)
            {
                await pageIterator.ResumeAsync(token);
            }

            return servicePrincipals;
        });

        return servicePrincipals;
    }

    private async Task<List<Application>> GetApplicationsAsync(CancellationToken token = default)
    {
        var getApplicationsResponse = await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            var progress = ctx.AddTask("Fetching applications");

            var getApplicationsResponse = await graphClient.Applications.GetAsync(cancellationToken: token);

            var applications = new List<Application>();

            var pageIterator = PageIterator<Application, ApplicationCollectionResponse>.CreatePageIterator(
                graphClient,
                getApplicationsResponse,
                (sp) =>
                {
                    progress.Increment(0.5);
                    applications.Add(sp);
                    return Task.FromResult(true);
                });

            await pageIterator.IterateAsync(token);

            while (pageIterator.State != PagingState.Complete)
            {
                await pageIterator.ResumeAsync(token);
            }

            return applications;
        });

        return getApplicationsResponse;
    }


    private async Task<ServicePrincipal> GetServicePrincipalAsync(string displayName, CancellationToken token = default)
    {
        var getServicePrincipalResponse = await AnsiConsole.Progress().StartAsync(async ctx =>
        {
            var response = await graphClient.ServicePrincipals.GetAsync(
                (requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = $"displayName eq '{displayName}'";
                }, token);

            var progress = ctx.AddTask("Fetching service principal");

            var servicePrincipals = new List<ServicePrincipal>();

            var pageIterator = PageIterator<ServicePrincipal, ServicePrincipalCollectionResponse>.CreatePageIterator(
                graphClient,
                response,
                (sp) =>
                {
                    progress.Increment(0.5);
                    servicePrincipals.Add(sp);
                    return Task.FromResult(true);
                });

            await pageIterator.IterateAsync(token);

            while (pageIterator.State != PagingState.Complete)
            {
                await pageIterator.ResumeAsync(token);
            }

            return servicePrincipals;
        });

        return getServicePrincipalResponse.SingleOrDefault();
    }


    private string[] FilterWithFuzzySearch(string[] options, string customText)
    {
        var filter = "";
        var table = new Table();
        table.AddColumn("Name");

        ConsoleKeyInfo key;
        do
        {
            table.Rows.Clear();

            FuzzySharp.Process.ExtractTop(filter, options, limit: 10)
                .Select(x => x.Value)
                .ToList()
                .ForEach(x => table.AddRow(new Text(x)));

            AnsiConsole.Clear();
            AnsiConsole.Write(table);
            AnsiConsole.Write(new Rule());
            AnsiConsole.Write(new Text($"Please type in the name of the {customText} to filter the results: " + filter));

            ProcessKey();

            void ProcessKey()
            {
                key = System.Console.ReadKey(true);

                if (key.Key == ConsoleKey.Backspace && filter.Length > 0)
                {
                    filter = filter.Substring(0, filter.Length - 1);
                }
                else if (key.Key != ConsoleKey.Enter && !char.IsControl(key.KeyChar) || key.Key == ConsoleKey.Spacebar)
                {
                    filter += key.KeyChar;
                }
                else if (key.Key is ConsoleKey.Escape)
                {
                    throw new OperationCanceledException();
                }
                else if (key.Key is not ConsoleKey.Enter)
                {
                    ProcessKey();
                }
            }
        } while (key.Key != ConsoleKey.Enter);

        return FuzzySharp.Process.ExtractTop(filter, options, limit: 10)
            .Select(x => x.Value)
            .ToArray();
    }
}