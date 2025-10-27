using HotWind.Cli.Models;
using HotWind.Cli.Services;
using Spectre.Console;

namespace HotWind.Cli.Commands;

public class GenerateRatesCommand
{
    private readonly IApiClient _apiClient;

    public GenerateRatesCommand(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold yellow]Generate Exchange Rates[/]");
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

            var days = (endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
            AnsiConsole.MarkupLine($"This will generate rates for [bold]{days}[/] days");

            if (!AnsiConsole.Confirm("Proceed?"))
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
                return;
            }

            var request = new GenerateRatesRequest
            {
                StartDate = startDate,
                EndDate = endDate
            };

            int count = 0;

            await AnsiConsole.Status()
                .StartAsync("Generating exchange rates...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    count = await _apiClient.GenerateExchangeRatesAsync(request);
                });

            AnsiConsole.MarkupLine($"[green]Successfully generated {count} exchange rates[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}
