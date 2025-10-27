using HotWind.Cli.Commands;
using HotWind.Cli.Services;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace HotWind.Cli;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5280";
        var apiClient = new ApiClient(apiBaseUrl);

        // Display banner
        ShowBanner();

        // Main menu loop
        bool running = true;

        while (running)
        {
            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold yellow]Main Menu[/]")
                    .PageSize(10)
                    .AddChoices(new[]
                    {
                        "Create Invoice",
                        "Stock Report",
                        "Price List Report",
                        "Currency Translation Report",
                        "Generate Exchange Rates",
                        "Exit"
                    }));

            AnsiConsole.Clear();
            ShowBanner();

            switch (choice)
            {
                case "Create Invoice":
                    var createInvoiceCommand = new CreateInvoiceCommand(apiClient);
                    await createInvoiceCommand.ExecuteAsync();
                    break;

                case "Stock Report":
                    var stockReportCommand = new StockReportCommand(apiClient);
                    await stockReportCommand.ExecuteAsync();
                    break;

                case "Price List Report":
                    var priceListReportCommand = new PriceListReportCommand(apiClient);
                    await priceListReportCommand.ExecuteAsync();
                    break;

                case "Currency Translation Report":
                    var currencyReportCommand = new CurrencyReportCommand(apiClient);
                    await currencyReportCommand.ExecuteAsync();
                    break;

                case "Generate Exchange Rates":
                    var generateRatesCommand = new GenerateRatesCommand(apiClient);
                    await generateRatesCommand.ExecuteAsync();
                    break;

                case "Exit":
                    running = false;
                    AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                    break;
            }

            if (running)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(true);
                AnsiConsole.Clear();
                ShowBanner();
            }
        }
    }

    static void ShowBanner()
    {
        var banner = new FigletText("HotWind")
            .LeftJustified()
            .Color(Color.Orange1);

        AnsiConsole.Write(banner);
        AnsiConsole.MarkupLine("[dim]Industrial Heating Equipment Retail Management System[/]");
        AnsiConsole.WriteLine();
    }
}
