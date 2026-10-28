// Classic JScript (ECMAScript 3) - no JSON.parse, no modern array methods,
// no String.trim, no let/const, no arrow functions.

function calculate(jsonString, answers) {
    // Parse the part A output with the host-provided parser (not JSON.parse).
    var parsed = JsonParser.parse(jsonString);

    // Take just the leases array from the parsed result.
    var leases = parsed.leases;

    // Build a single input object combining the parsed leases and the answers.
    var input = {
        leases: leases,
        Framework: answers.Framework,
        EntitySize: answers.EntitySize,
        HasSubleases: answers.HasSubleases
    };

    // Half-away-from-zero rounding to 2 decimals. ES3 Math.round rounds .5
    // toward +Infinity and mishandles negatives, and closing values can be
    // negative, so do it explicitly.
    function round2(value) {
        var scaled = value * 100;
        var rounded;
        if (scaled >= 0) {
            rounded = Math.floor(scaled + 0.5);
        } else {
            rounded = Math.ceil(scaled - 0.5);
        }
        return rounded / 100;
    }

    var result = {};

    // LeaseCount: the number of leases.
    result.LeaseCount = input.leases.length;

    // HasLeases: "Yes" if there is at least one lease.
    result.HasLeases = result.LeaseCount > 0 ? "Yes" : "No";

    // MixedCurrency: "Yes" if the leases use more than one distinct currency.
    var mixed = false;
    var firstCurrency = null;
    var i;
    for (i = 0; i < input.leases.length; i++) {
        var currency = input.leases[i].currency;
        if (firstCurrency === null) {
            firstCurrency = currency;
        } else if (currency !== firstCurrency) {
            mixed = true;
        }
    }
    result.MixedCurrency = mixed ? "Yes" : "No";

    // Spec defect: SummaryRequired is defined via KpiRequired, which does not
    // exist in the inputs, so it can never be true. Fixed as always "No".
    result.SummaryRequired = "No";

    // TotalClosing: sum of (Opening + Additions - Depreciation - Disposals).
    // Null when MixedCurrency is "Yes" (brief: not calculated in that case).
    if (result.MixedCurrency === "Yes") {
        result.TotalClosing = null;
    } else {
        var total = 0;
        for (i = 0; i < input.leases.length; i++) {
            var lease = input.leases[i];
            total = total + lease.opening + lease.additions - lease.depreciation - lease.disposals;
        }
        result.TotalClosing = round2(total);
    }

    // AverageClosing: TotalClosing / LeaseCount.
    // Null if TotalClosing is null or LeaseCount is 0.
    if (result.TotalClosing === null || result.LeaseCount === 0) {
        result.AverageClosing = null;
    } else {
        result.AverageClosing = round2(result.TotalClosing / result.LeaseCount);
    }

    // ShowRouDisclosure: "Yes" if HasLeases is "Yes" and Framework is "IFRS".
    if (result.HasLeases === "Yes" && input.Framework === "IFRS") {
        result.ShowRouDisclosure = "Yes";
    } else {
        result.ShowRouDisclosure = "No";
    }

    // Spec defect: the OR with EntitySize means this can be "Yes" even with no
    // leases (Large entity alone triggers it). Kept exactly as the brief's
    // bracketing specifies: (HasLeases and IFRS) or (Large).
    if ((result.HasLeases === "Yes" && input.Framework === "IFRS") || (input.EntitySize === "Large")) {
        result.ShowMaturityAnalysis = "Yes";
    } else {
        result.ShowMaturityAnalysis = "No";
    }

    // Spec defect: the brief ties sublease disclosure to HasLeases being "No",
    // so it only shows when there are no leases - counterintuitive, but
    // implemented exactly as specified.
    if (input.HasSubleases === "Yes" && result.HasLeases === "No") {
        result.ShowSubleaseDisclosure = "Yes";
    } else {
        result.ShowSubleaseDisclosure = "No";
    }

    // Spec defect: ShowCashOutflow is defined via TotalCashOutflow, which is
    // not present in the inputs, so it cannot be calculated. Fixed as null.
    result.ShowCashOutflow = null;

    return result;
}
