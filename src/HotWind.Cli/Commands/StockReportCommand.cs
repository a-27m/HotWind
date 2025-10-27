using HotWind.Cli.Services;
using HotWind.Cli.UI;
using Spectre.Console;

namespace HotWind.Cli.Commands;

public class StockReportCommand
{
    private readonly IApiClient _apiClient;

    public StockReportCommand(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            AnsiConsole.Status()
                .Start("Loading stock report...", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    var report = _apiClient.GetStockReportAsync().GetAwaiter().GetResult();
                    TableRenderer.RenderStockReport(report);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}
