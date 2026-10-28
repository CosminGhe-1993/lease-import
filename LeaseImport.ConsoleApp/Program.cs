using System.Text.Json;
using LeaseImport.ConsoleApp;

var path = args.Length > 0 ? args[0] : "leases.csv";

if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {path}");
    return;
}

var result = new LeaseParser().Parse(path);

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

var json = JsonSerializer.Serialize(result, options);
File.WriteAllText("output.json", json);

Console.WriteLine($"Parsed {result.Leases.Count} lease(s) and found {result.Problems.Count} problem(s).");
Console.WriteLine("Output written to output.json");
