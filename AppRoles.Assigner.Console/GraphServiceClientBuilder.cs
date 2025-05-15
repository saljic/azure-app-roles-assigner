using Azure.Identity;
using Microsoft.Graph;
using Spectre.Console;

namespace AppRoles.Assigner.Console;

public sealed class GraphServiceClientBuilder
{
    public GraphServiceClient Build()
    {
        var selectedCredential = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select the authentication method: ")
                .AddChoices("azure-cli", "interactive-browser"));

        var defaultAzureCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeSharedTokenCacheCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeInteractiveBrowserCredential = selectedCredential != "interactive-browser",
            ExcludeEnvironmentCredential = true,
            ExcludeManagedIdentityCredential = true,
            ExcludeAzureCliCredential = selectedCredential != "azure-cli",
            ExcludeAzurePowerShellCredential = true
        });

        return new GraphServiceClient(defaultAzureCredential);
    }
}