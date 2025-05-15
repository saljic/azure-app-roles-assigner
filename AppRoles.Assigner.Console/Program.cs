using AppRoles.Assigner.Console;
using Spectre.Console;

var cancellationTokenSource = new CancellationTokenSource();

try
{
    var graphServiceClient = new GraphServiceClientBuilder().Build();

    var assigner = new AppRoleAssigner(graphServiceClient);

    await assigner.AssignAsync(cancellationTokenSource.Token);
}
catch (Exception e)
{
    if (e is OperationCanceledException)
    {
        AnsiConsole.MarkupLine("Operation was canceled by the user.");
        return;
    }

    AnsiConsole.WriteException(e);
    throw;
}
finally
{
    cancellationTokenSource.Dispose();
}