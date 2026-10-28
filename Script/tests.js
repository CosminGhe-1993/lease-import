// Hand-written test cases for calculate(). Run via run.wsf with cscript. ES3 only.

// Prints every field of a calculate() result. A plain for-loop over a list of
// names (no modern array methods). null prints as the string "null".
function dump(label, result) {
    var names = [
        "LeaseCount",
        "HasLeases",
        "MixedCurrency",
        "SummaryRequired",
        "TotalClosing",
        "AverageClosing",
        "ShowRouDisclosure",
        "ShowMaturityAnalysis",
        "ShowSubleaseDisclosure",
        "ShowCashOutflow"
    ];
    WScript.Echo("=== " + label + " ===");
    var i;
    for (i = 0; i < names.length; i++) {
        var name = names[i];
        WScript.Echo("  " + name + ": " + result[name]);
    }
    WScript.Echo("");
}

// Case 1: two currencies -> MixedCurrency "Yes", TotalClosing/AverageClosing null.
var twoCurrencies =
    '{ "leases": [' +
    '  { "leaseId": "L-1", "currency": "DKK", "opening": 1000, "additions": 0, "depreciation": 100, "disposals": 0 },' +
    '  { "leaseId": "L-2", "currency": "EUR", "opening": 500, "additions": 0, "depreciation": 50, "disposals": 0 }' +
    '], "problems": [] }';

dump("Two currencies", calculate(twoCurrencies, {
    Framework: "IFRS",
    EntitySize: "Large",
    HasSubleases: "No"
}));

// Case 2: single lease, one currency -> Total and Average computed.
// Closing = 1000 + 50 - 100 - 0 = 950.
var singleLease =
    '{ "leases": [' +
    '  { "leaseId": "L-1", "currency": "DKK", "opening": 1000, "additions": 50, "depreciation": 100, "disposals": 0 }' +
    '], "problems": [] }';

dump("Single lease", calculate(singleLease, {
    Framework: "IFRS",
    EntitySize: "Small",
    HasSubleases: "No"
}));

// Case 3: empty leases -> HasLeases "No", AverageClosing null, ShowSubleaseDisclosure "Yes".
var emptyLeases = '{ "leases": [], "problems": [] }';

dump("Empty leases", calculate(emptyLeases, {
    Framework: "LocalGAAP",
    EntitySize: "Micro",
    HasSubleases: "Yes"
}));

// Case 4: negative closing (depreciation above opening) to check negative
// rounding. Closing = 100 + 0 - 100.125 - 0 = -0.125 -> half away from zero -> -0.13.
var negativeClosing =
    '{ "leases": [' +
    '  { "leaseId": "L-1", "currency": "DKK", "opening": 100, "additions": 0, "depreciation": 100.125, "disposals": 0 }' +
    '], "problems": [] }';

dump("Negative closing", calculate(negativeClosing, {
    Framework: "IFRS",
    EntitySize: "Small",
    HasSubleases: "No"
}));

// Case 5: amounts that need rounding to 2 decimals.
// Closing = 1000.125 -> half away from zero -> 1000.13.
var needsRounding =
    '{ "leases": [' +
    '  { "leaseId": "L-1", "currency": "DKK", "opening": 1000.125, "additions": 0, "depreciation": 0, "disposals": 0 }' +
    '], "problems": [] }';

dump("Needs rounding", calculate(needsRounding, {
    Framework: "IFRS",
    EntitySize: "Small",
    HasSubleases: "No"
}));

// Case 6: Large entity with Framework LocalGAAP and a lease, so
// ShowMaturityAnalysis is "Yes" only through the EntitySize branch
// (ShowRouDisclosure stays "No" because the framework is not IFRS).
var largeLocalGaap =
    '{ "leases": [' +
    '  { "leaseId": "L-1", "currency": "DKK", "opening": 1000, "additions": 0, "depreciation": 100, "disposals": 0 }' +
    '], "problems": [] }';

dump("Large + LocalGAAP", calculate(largeLocalGaap, {
    Framework: "LocalGAAP",
    EntitySize: "Large",
    HasSubleases: "No"
}));

// Case 7: several leases in the same currency, to check the total sums.
// 900 + 2100 + 250 = 3250; average 3250 / 3 = 1083.333... -> 1083.33.
var multipleLeases =
    '{ "leases": [' +
    '  { "leaseId": "L-1", "currency": "DKK", "opening": 1000, "additions": 0, "depreciation": 100, "disposals": 0 },' +
    '  { "leaseId": "L-2", "currency": "DKK", "opening": 2000, "additions": 500, "depreciation": 300, "disposals": 100 },' +
    '  { "leaseId": "L-3", "currency": "DKK", "opening": 300, "additions": 0, "depreciation": 50, "disposals": 0 }' +
    '], "problems": [] }';

dump("Multiple same currency", calculate(multipleLeases, {
    Framework: "IFRS",
    EntitySize: "Medium",
    HasSubleases: "No"
}));
