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
            await AnsiConsole.Status()
                .StartAsync("Loading stock report...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    var report = await _apiClient.GetStockReportAsync();
                    TableRenderer.RenderStockReport(report);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}
