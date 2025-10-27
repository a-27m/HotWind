using HotWind.Cli.Models;
using Spectre.Console;

namespace HotWind.Cli.UI;

public static class TableRenderer
{
    public static void RenderStockReport(List<StockReportItem> items)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.Title("[bold yellow]Stock Report[/]");

        table.AddColumn(new TableColumn("[bold]SKU[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Model[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Manufacturer[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Stock[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Lots[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Avg Cost (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]List Price (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Profit (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Margin %[/]").RightAligned());

        foreach (var item in items)
        {
            var profitColor = item.PotentialProfit >= 0 ? "green" : "red";

            table.AddRow(
                item.Sku,
                item.ModelName.Length > 30 ? item.ModelName.Substring(0, 27) + "..." : item.ModelName,
                item.Manufacturer,
                item.StockLevel.ToString("N0"),
                item.LotCount.ToString(),
                item.WeightedAvgPurchasePriceUah.ToString("N2"),
                item.ListPriceUah.ToString("N2"),
                $"[{profitColor}]{item.PotentialProfit:N2}[/]",
                $"[{profitColor}]{item.ProfitMarginPercent:N2}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total items:[/] {items.Count}");
    }

    public static void RenderPriceListReport(List<PriceListReportItem> items)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.Title("[bold yellow]Price List Report[/]");

        table.AddColumn(new TableColumn("[bold]SKU[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Model[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Manufacturer[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Stock[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Lot Value (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Market Value (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Difference (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Difference %[/]").RightAligned());

        foreach (var item in items)
        {
            var diffColor = item.ValueDifferenceUah >= 0 ? "green" : "red";

            table.AddRow(
                item.Sku,
                item.ModelName.Length > 30 ? item.ModelName.Substring(0, 27) + "..." : item.ModelName,
                item.Manufacturer,
                item.StockLevel.ToString("N0"),
                item.WeightedLotValueUah.ToString("N2"),
                item.CurrentMarketValueUah.ToString("N2"),
                $"[{diffColor}]{item.ValueDifferenceUah:N2}[/]",
                $"[{diffColor}]{item.ValueDifferencePercent:N2}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total items:[/] {items.Count}");
    }

    public static void RenderCurrencyTranslationReport(List<CurrencyTranslationReportItem> items, DateOnly startDate, DateOnly endDate)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.Title($"[bold yellow]Currency Translation Report ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})[/]");

        table.AddColumn(new TableColumn("[bold]SKU[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Model[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Manufacturer[/]").LeftAligned());
        table.AddColumn(new TableColumn("[bold]Units Sold[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Historical (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Current (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]Difference (UAH)[/]").RightAligned());
        table.AddColumn(new TableColumn("[bold]FX Impact %[/]").RightAligned());

        foreach (var item in items)
        {
            var diffColor = item.ValueDifferenceUah >= 0 ? "green" : "red";

            table.AddRow(
                item.Sku,
                item.ModelName.Length > 30 ? item.ModelName.Substring(0, 27) + "..." : item.ModelName,
                item.Manufacturer,
                item.TotalUnitsSold.ToString("N0"),
                item.HistoricalValueUah.ToString("N2"),
                item.CurrentValueUah.ToString("N2"),
                $"[{diffColor}]{item.ValueDifferenceUah:N2}[/]",
                $"[{diffColor}]{item.ExchangeRateImpactPercent:N2}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Total items:[/] {items.Count}");
    }
}
