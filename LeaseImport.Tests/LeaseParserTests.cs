using LeaseImport.ConsoleApp;
using LeaseImport.ConsoleApp.Models;

namespace LeaseImport.Tests;

public class LeaseParserTests
{
    private const string Header =
        "LeaseId,AssetClass,Description,Currency,Opening,Additions,Depreciation,Disposals";

    // Writes the CSV content to a temp file, parses it, then deletes the file.
    private static ParseResult ParseContent(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            File.WriteAllText(path, content);
            return new LeaseParser().Parse(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ValidFile_AllRowsPass_LeasesPopulated_NoProblems()
    {
        var csv = string.Join("\n",
            Header,
            "L-1,Property,Office,DKK,1000.00,0,100.00,0",
            "L-2,Vehicles,Fleet,EUR,2000,50,200,0");

        var result = ParseContent(csv);

        Assert.Equal(2, result.Leases.Count);
        Assert.Empty(result.Problems);
    }

    [Theory]
    [InlineData(",Property,Office,DKK,1000,0,100,0")]          // empty LeaseId
    [InlineData("L-1,Property,Office,,1000,0,100,0")]          // empty Currency
    [InlineData("L-1,Property,Office,DKK,,0,100,0")]           // empty figure (Opening)
    public void EmptyRequiredField_GivesProblem(string row)
    {
        var result = ParseContent(string.Join("\n", Header, row));

        Assert.Empty(result.Leases);
        Assert.Contains(result.Problems, p => p.Issue == "required value is missing");
    }

    [Fact]
    public void FigureThatIsNotANumber_GivesProblem()
    {
        // "DKK" in the Opening column.
        var csv = string.Join("\n", Header, "L-1,Property,Office,DKK,DKK,0,100,0");

        var result = ParseContent(csv);

        Assert.Empty(result.Leases);
        Assert.Contains(result.Problems, p => p.Field == "Opening" && p.Issue == "not a valid decimal");
    }

    [Fact]
    public void NegativeFigure_GivesProblem()
    {
        var csv = string.Join("\n", Header, "L-1,Property,Office,DKK,-5,0,100,0");

        var result = ParseContent(csv);

        Assert.Empty(result.Leases);
        Assert.Contains(result.Problems, p => p.Field == "Opening" && p.Issue == "value is negative");
    }

    [Fact]
    public void InvalidCurrency_GivesProblem()
    {
        var csv = string.Join("\n", Header, "L-1,Property,Office,euro,1000,0,100,0");

        var result = ParseContent(csv);

        Assert.Empty(result.Leases);
        Assert.Contains(result.Problems, p => p.Field == "Currency" && p.Issue == "not a valid ISO-4217 currency code");
    }

    [Fact]
    public void DuplicateLeaseId_GivesProblem()
    {
        var csv = string.Join("\n",
            Header,
            "L-1,Property,Office,DKK,1000,0,100,0",
            "L-1,Vehicles,Fleet,EUR,2000,0,200,0");

        var result = ParseContent(csv);

        Assert.Single(result.Leases);                 // only the first is kept
        Assert.Contains(result.Problems, p => p.Issue == "duplicate LeaseId");
    }

    [Fact]
    public void WrongNumberOfColumns_GivesProblem()
    {
        // Only 7 columns (Disposals missing from the row).
        var csv = string.Join("\n", Header, "L-1,Property,Office,DKK,1000,0,100");

        var result = ParseContent(csv);

        Assert.Empty(result.Leases);
        var problem = Assert.Single(result.Problems);
        Assert.Equal("row does not have 8 columns", problem.Issue);
        Assert.Equal("L-1", problem.LeaseId);          // LeaseId kept because the row reached it
    }

    [Fact]
    public void TooManyColumns_GivesProblem()
    {
        // 9 columns (an extra trailing field).
        var csv = string.Join("\n", Header, "L-1,Property,Office,DKK,1000,0,100,0,extra");

        var result = ParseContent(csv);

        Assert.Empty(result.Leases);
        var problem = Assert.Single(result.Problems);
        Assert.Equal("row does not have 8 columns", problem.Issue);
        Assert.Equal("L-1", problem.LeaseId);          // LeaseId kept because the row reached it
    }

    [Fact]
    public void RowWithSeveralProblems_ReportsAllOfThem()
    {
        // Empty LeaseId + invalid currency + negative Opening = 3 problems.
        var csv = string.Join("\n", Header, ",Property,Office,euro,-5,0,100,0");

        var result = ParseContent(csv);

        Assert.Empty(result.Leases);
        Assert.Equal(3, result.Problems.Count);
        Assert.Contains(result.Problems, p => p.Field == "LeaseId" && p.Issue == "required value is missing");
        Assert.Contains(result.Problems, p => p.Field == "Currency" && p.Issue == "not a valid ISO-4217 currency code");
        Assert.Contains(result.Problems, p => p.Field == "Opening" && p.Issue == "value is negative");
    }

    [Fact]
    public void ColumnsInDifferentOrder_AreParsedCorrectly()
    {
        // Header (and rows) in reverse column order.
        var csv = string.Join("\n",
            "Disposals,Depreciation,Additions,Opening,Currency,Description,AssetClass,LeaseId",
            "0,10,5,100,EUR,Desc,Property,L-9");

        var result = ParseContent(csv);

        Assert.Empty(result.Problems);
        var lease = Assert.Single(result.Leases);
        Assert.Equal("L-9", lease.LeaseId);
        Assert.Equal("EUR", lease.Currency);
        Assert.Equal(100m, lease.Opening);
        Assert.Equal(5m, lease.Additions);
        Assert.Equal(10m, lease.Depreciation);
        Assert.Equal(0m, lease.Disposals);
    }

    [Fact]
    public void HeaderMissingColumn_GivesError()
    {
        // Disposals dropped from the header.
        var csv = "LeaseId,AssetClass,Description,Currency,Opening,Additions,Depreciation";

        var result = ParseContent(csv);

        Assert.Empty(result.Leases);
        Assert.Contains(result.Problems, p => p.Field == "Disposals" && p.Issue == "missing column");
    }

    [Fact]
    public void EmptyFile_GivesOneFileIsEmptyProblem()
    {
        var result = ParseContent("");

        Assert.Empty(result.Leases);
        var problem = Assert.Single(result.Problems);
        Assert.Equal("file is empty", problem.Issue);
    }

    [Fact]
    public void HeaderOnlyFile_IsValid_EmptyLeasesAndProblems()
    {
        var result = ParseContent(Header);

        Assert.Empty(result.Leases);
        Assert.Empty(result.Problems);
    }

    [Fact]
    public void WhitespaceAroundValues_IsTrimmed_RowPasses()
    {
        var csv = string.Join("\n",
            Header,
            "  L-1  ,  Property  ,  Office  ,  dkk  ,  1000.50  ,  0  ,  100.00  ,  0  ");

        var result = ParseContent(csv);

        Assert.Empty(result.Problems);
        var lease = Assert.Single(result.Leases);
        Assert.Equal("L-1", lease.LeaseId);
        Assert.Equal("DKK", lease.Currency);           // trimmed + upper-cased
        Assert.Equal(1000.50m, lease.Opening);
    }
}
