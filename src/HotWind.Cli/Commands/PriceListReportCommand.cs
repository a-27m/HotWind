using HotWind.Cli.Services;
using HotWind.Cli.UI;
using Spectre.Console;

namespace HotWind.Cli.Commands;

public class PriceListReportCommand
{
    private readonly IApiClient _apiClient;

    public PriceListReportCommand(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            AnsiConsole.Status()
                .Start("Loading price list report...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    var report = _apiClient.GetPriceListReportAsync().GetAwaiter().GetResult();
                    TableRenderer.RenderPriceListReport(report);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}
