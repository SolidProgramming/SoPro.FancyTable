namespace SoPro.FancyTable.BrowserBenchmarks;

public sealed record Fixture(string Kind, int Size, bool Paged, bool Templates)
{
    public string Id => $"{Kind}-{Size}-{(Paged ? "paged" : "all")}-{(Templates ? "templates" : "text")}";
    public string Query => $"?kind={Kind}&size={Size}&paged={Paged}&templates={Templates}";
    public string[] Actions => Kind == "flat"
        ? Paged ? ["mount", "sort", "search", "page"] : ["mount", "sort", "search"]
        : ["mount", "expand", "collapse", "expand-all", "collapse-all"];

    public string[] Expected(string action)
    {
        if (Kind == "flat")
        {
            IEnumerable<int> ids = Enumerable.Range(0, Size).Reverse();
            if (action == "sort") ids = ids.Order();
            if (action == "search") ids = ids.Where(i => i % 10 == 0);
            if (action == "page") ids = ids.Skip(50);
            if (Paged) ids = ids.Take(50);
            return ids.Select(i => $"f-{i:D5}").ToArray();
        }
        var result = new List<string>();
        void Add(string prefix, int remaining)
        {
            for (int i = 0; i < 10; i++)
            {
                string id = $"{prefix}-{i}";
                result.Add(id);
                if (remaining > 1 && (action == "expand-all" || (action == "expand" && id == "t-0")))
                    Add(id, action == "expand" ? 1 : remaining - 1);
            }
        }
        Add("t", Size);
        return result.ToArray();
    }

    public object ExpectedDom(string action)
    {
        string[] ids = Expected(action);
        return new { count = ids.Length, first = ids[0], last = ids[^1],
            expanded = action switch
            {
                "expand" or "expand-all" => (bool?)true,
                "collapse" or "collapse-all" => false,
                _ => null
            } };
    }

    public static IReadOnlyList<Fixture> All() =>
        (from size in new[] { 1000, 10000 }
         from paged in new[] { true, false }
         from templates in new[] { false, true }
         select new Fixture("flat", size, paged, templates))
        .Concat(from depth in new[] { 3, 4 }
                from templates in new[] { false, true }
                select new Fixture("tree", depth, false, templates)).ToArray();
}

public sealed record Profile(string Name, int Warmups, int Measurements)
{
    public static Profile Parse(string name) => name.ToLowerInvariant() switch
    {
        "dry" => new("Dry", 0, 1),
        "short" => new("Short", 2, 5),
        "full" => new("Full", 5, 30),
        _ => throw new ArgumentException("Profile must be Dry, Short, or Full.")
    };
}
