using HotWind.Cli.Models;
using HotWind.Cli.Services;
using Spectre.Console;

namespace HotWind.Cli.Commands;

public class CreateInvoiceCommand
{
    private readonly IApiClient _apiClient;

    public CreateInvoiceCommand(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold yellow]Create New Invoice[/]");
            AnsiConsole.WriteLine();

            // Customer selection
            var searchTerm = AnsiConsole.Ask<string>("Search for customer (company name):");
            var customers = await _apiClient.GetCustomersAsync(searchTerm);

            if (customers.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No customers found[/]");
                return;
            }

            var customerChoices = customers.Take(10).Select(c =>
                $"{c.CompanyName} (ID: {c.CustomerId})").ToList();

            var selectedCustomer = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select [green]customer[/]:")
                    .PageSize(10)
                    .AddChoices(customerChoices));

            var customerId = customers.First(c =>
                $"{c.CompanyName} (ID: {c.CustomerId})" == selectedCustomer).CustomerId;

            // Invoice date
            var invoiceDate = AnsiConsole.Prompt(
                new TextPrompt<DateOnly>("Invoice date (yyyy-MM-dd):")
                    .DefaultValue(DateOnly.FromDateTime(DateTime.Now)));

            // Invoice lines
            var lines = new List<InvoiceLine>();
            bool addingLines = true;

            while (addingLines)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Line item #{lines.Count + 1}[/]");

                // Model selection
                var modelSearch = AnsiConsole.Ask<string>("Search for heater model (SKU or name):");
                var models = await _apiClient.GetModelsAsync(modelSearch, inStockOnly: true);

                if (models.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]No models found in stock[/]");
                    continue;
                }

                var modelChoices = models.Take(10).Select(m =>
                    $"{m.Sku} - {m.ModelName} (Stock: {m.StockLevel})").ToList();

                var selectedModel = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select [green]model[/]:")
                        .PageSize(10)
                        .AddChoices(modelChoices));

                var model = models.First(m =>
                    $"{m.Sku} - {m.ModelName} (Stock: {m.StockLevel})" == selectedModel);

                // Quantity
                var quantity = AnsiConsole.Prompt(
                    new TextPrompt<int>("Quantity:")
                        .DefaultValue(1)
                        .Validate(q =>
                        {
                            if (q <= 0) return ValidationResult.Error("[red]Quantity must be positive[/]");
                            if (q > model.StockLevel) return ValidationResult.Error($"[red]Only {model.StockLevel} in stock[/]");
                            return ValidationResult.Success();
                        }));

                // Price
                var suggestedPrice = model.ListPriceUah ?? 0;
                var unitPrice = AnsiConsole.Prompt(
                    new TextPrompt<decimal>($"Unit price (UAH) [suggested: {suggestedPrice:N2}]:")
                        .DefaultValue(suggestedPrice)
                        .Validate(p => p >= 0
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Price must be non-negative[/]")));

                lines.Add(new InvoiceLine
                {
                    Sku = model.Sku,
                    Quantity = quantity,
                    UnitPrice = unitPrice
                });

                AnsiConsole.MarkupLine($"[green]Added:[/] {quantity}x {model.ModelName} @ {unitPrice:N2} UAH");

                addingLines = AnsiConsole.Confirm("Add another line item?", false);
            }

            if (lines.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No line items added. Invoice not created.[/]");
                return;
            }

            // Notes
            var notes = AnsiConsole.Ask<string>("Notes (optional, press Enter to skip):", string.Empty);

            // Confirmation
            AnsiConsole.WriteLine();
            var total = lines.Sum(l => l.Quantity * l.UnitPrice);
            AnsiConsole.MarkupLine($"[bold]Invoice Summary:[/]");
            AnsiConsole.MarkupLine($"Customer: {selectedCustomer}");
            AnsiConsole.MarkupLine($"Date: {invoiceDate:yyyy-MM-dd}");
            AnsiConsole.MarkupLine($"Line items: {lines.Count}");
            AnsiConsole.MarkupLine($"[bold]Total: {total:N2} UAH[/]");

            if (!AnsiConsole.Confirm("Create this invoice?"))
            {
                AnsiConsole.MarkupLine("[yellow]Invoice creation cancelled[/]");
                return;
            }

            // Create invoice
            var request = new CreateInvoiceRequest
            {
                CustomerId = customerId,
                InvoiceDate = invoiceDate,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
                Lines = lines
            };

            await AnsiConsole.Status()
                .StartAsync("Creating invoice...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    await _apiClient.CreateInvoiceAsync(request);
                });

            AnsiConsole.MarkupLine("[green]Invoice created successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
}
