using System.Globalization;
using LeaseImport.ConsoleApp.Models;

namespace LeaseImport.ConsoleApp;

public class LeaseParser
{
    private static readonly string[] ExpectedColumns =
    {
        "LeaseId", "AssetClass", "Description", "Currency",
        "Opening", "Additions", "Depreciation", "Disposals"
    };

    private static readonly string[] FigureColumns =
    {
        "Opening", "Additions", "Depreciation", "Disposals"
    };

    private static readonly HashSet<string> ValidCurrencies = BuildValidCurrencies();

    public ParseResult Parse(string filePath)
    {
        var result = new ParseResult();
        var seenLeaseIds = new HashSet<string>();

        var lines = File.ReadAllLines(filePath);

        // All lines that actually have content.
        var contentLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        // Completely empty file (not even a header).
        if (contentLines.Count == 0)
        {
            result.Problems.Add(new Problem { Issue = "file is empty" });
            return result;
        }

        // Header row -> column name to index map.
        var header = contentLines[0].Split(',');
        var map = new Dictionary<string, int>();
        for (int i = 0; i < header.Length; i++)
        {
            map[header[i].Trim()] = i;
        }

        // Any expected column missing from the header is an error.
        bool headerValid = true;
        foreach (var column in ExpectedColumns)
        {
            if (!map.ContainsKey(column))
            {
                result.Problems.Add(new Problem { Field = column, Issue = "missing column" });
                headerValid = false;
            }
        }

        // Without a complete mapping the rows can't be read reliably.
        if (!headerValid)
        {
            return result;
        }

        // Data rows.
        for (int i = 1; i < contentLines.Count; i++)
        {
            var fields = contentLines[i].Split(',');

            // Row must have exactly 8 columns.
            if (fields.Length != 8)
            {
                var problem = new Problem { Field = "row", Issue = "row does not have 8 columns" };

                // Keep the LeaseId if the row is long enough to reach it.
                int idIndex = map["LeaseId"];
                if (fields.Length > idIndex)
                {
                    problem.LeaseId = fields[idIndex].Trim();
                }

                result.Problems.Add(problem);
                continue;
            }

            // Read the fields we keep, by mapped index.
            var leaseId = fields[map["LeaseId"]].Trim();
            var currency = fields[map["Currency"]].Trim();

            var rowProblems = new List<Problem>();

            // LeaseId required.
            if (string.IsNullOrEmpty(leaseId))
            {
                rowProblems.Add(new Problem { LeaseId = leaseId, Field = "LeaseId", Value = leaseId, Issue = "required value is missing" });
            }

            // Currency required + valid ISO-4217.
            var currencyUpper = currency.ToUpperInvariant();
            if (string.IsNullOrEmpty(currency))
            {
                rowProblems.Add(new Problem { LeaseId = leaseId, Field = "Currency", Value = currency, Issue = "required value is missing" });
            }
            else if (!ValidCurrencies.Contains(currencyUpper))
            {
                rowProblems.Add(new Problem { LeaseId = leaseId, Field = "Currency", Value = currency, Issue = "not a valid ISO-4217 currency code" });
            }

            // The four figures.
            var figures = new Dictionary<string, decimal>();
            foreach (var column in FigureColumns)
            {
                var raw = fields[map[column]].Trim();

                if (string.IsNullOrEmpty(raw))
                {
                    rowProblems.Add(new Problem { LeaseId = leaseId, Field = column, Value = raw, Issue = "required value is missing" });
                    continue;
                }

                if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                {
                    rowProblems.Add(new Problem { LeaseId = leaseId, Field = column, Value = raw, Issue = "not a valid decimal" });
                    continue;
                }

                if (value < 0)
                {
                    rowProblems.Add(new Problem { LeaseId = leaseId, Field = column, Value = raw, Issue = "value is negative" });
                    continue;
                }

                figures[column] = value;
            }

            // Duplicate LeaseId.
            if (!string.IsNullOrEmpty(leaseId) && seenLeaseIds.Contains(leaseId))
            {
                rowProblems.Add(new Problem { LeaseId = leaseId, Field = "LeaseId", Value = leaseId, Issue = "duplicate LeaseId" });
            }

            // A row with any problem goes only to Problems.
            if (rowProblems.Count > 0)
            {
                result.Problems.AddRange(rowProblems);
                continue;
            }

            // Valid row becomes a Lease.
            result.Leases.Add(new Lease
            {
                LeaseId = leaseId,
                Currency = currencyUpper,
                Opening = figures["Opening"],
                Additions = figures["Additions"],
                Depreciation = figures["Depreciation"],
                Disposals = figures["Disposals"]
            });
            seenLeaseIds.Add(leaseId);
        }

        return result;
    }

    private static HashSet<string> BuildValidCurrencies()
    {
        var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                var region = new RegionInfo(culture.Name);
                currencies.Add(region.ISOCurrencySymbol);
            }
            catch (ArgumentException)
            {
                // Some cultures have no associated region; skip them.
            }
        }
        return currencies;
    }
}
