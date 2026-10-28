# Lease Import

A take-home exercise in two independent parts.

- **Part A (C#)** reads a CSV of leases, validates the rows, and writes a JSON file with the valid leases and a list of problems.
- **Part B (classic JScript, ECMAScript 3)** takes that JSON as a string and calculates the summary and disclosure fields.

The two parts are not wired together. Part A produces the JSON and Part B consumes it; how the JSON travels between them is out of scope, as the brief states.

See `Notes.txt` for the design decisions and the handling of the spec defects (F-04, F-08, F-09, F-10).

## Structure

```
LeaseImport.ConsoleApp/   Part A: CSV reader and JSON writer
LeaseImport.Tests/        Part A: xUnit tests
Script/                   Part B: calculate.js + test harness
leases.csv                sample input
output.json               sample output produced from leases.csv
Notes.txt                 design decisions and assumptions
```

## Running Part A

From the repository root:

```
dotnet run --project LeaseImport.ConsoleApp
```

It reads `leases.csv` by default (or a path passed as the first argument) and writes `output.json`.

## Running the Part A tests

```
dotnet test LeaseImport.Tests
```

## Running the Part B tests

Part B is classic JScript, run with Windows Script Host. From the `Script` folder:

```
cscript //nologo run.wsf
```

This loads the JSON parser shim, then `calculate.js`, then the hand-written test cases, and prints each result.
