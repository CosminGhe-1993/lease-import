// Shim for WSH, which has no built-in JSON. The JSON here is hand-written and
// trusted, so parse just evaluates the text wrapped in parentheses. ES3 only.

var JsonParser = {
    parse: function (text) {
        return eval("(" + text + ")");
    }
};
