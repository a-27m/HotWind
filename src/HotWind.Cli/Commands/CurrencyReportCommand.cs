using HotWind.Cli.Services;
using HotWind.Cli.UI;
using Spectre.Console;

namespace HotWind.Cli.Commands;

public class CurrencyReportCommand
{
    private readonly IApiClient _apiClient;

    public CurrencyReportCommand(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold yellow]Currency Translation Report[/]");
            AnsiConsole.WriteLine();

            var startDate = AnsiConsole.Prompt(
                new TextPrompt<DateOnly>("Enter start date (yyyy-MM-dd):")
                    .DefaultValue(DateOnly.FromDateTime(DateTime.Now.AddMonths(-1)))
                    .ValidationErrorMessage("[red]Invalid date format[/]"));

            var endDate = AnsiConsole.Prompt(
                new TextPrompt<DateOnly>("Enter end date (yyyy-MM-dd):")
                    .DefaultValue(DateOnly.FromDateTime(DateTime.Now))
                    .ValidationErrorMessage("[red]Invalid date format[/]"));

            if (startDate > endDate)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Start date must be before end date");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Loading currency translation report...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    var report = await _apiClient.GetCurrencyTranslationReportAsync(startDate, endDate);
                    TableRenderer.RenderCurrencyTranslationReport(report, startDate, endDate);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}
