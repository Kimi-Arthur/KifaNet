// See https://aka.ms/new-console-template for more information

var x = new Dictionary<string, string> {
    // { "", "ab" }
};

Console.WriteLine(x.Select(kv => (KeyValuePair<string, string>?) (kv))
    .MaxBy(kv => kv.Value.Key.Length) == null);
