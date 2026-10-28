namespace LeaseImport.ConsoleApp.Models;

public class ParseResult
{
    public List<Lease> Leases { get; set; } = new();
    public List<Problem> Problems { get; set; } = new();
}