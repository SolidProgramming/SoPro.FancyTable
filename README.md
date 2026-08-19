# SoPro.FancyTable

https://www.nuget.org/packages/SoPro.FancyTable

![Playground.png](https://github.com/SolidProgramming/SoPro.FancyTable/blob/ba84b575fafa1c86a7c4e7f074de32cb52be5c18/Playground.png)

## Overview

`FancyTable` is a reusable Blazor component that provides an interactive table experience with built-in support for searching, sorting, and column visibility management. It's designed to work with any data type through its generic `TItem` parameter.

`FancyTreeTable` extends the same column model and interaction patterns to hierarchical datasets, so parent/child structures can be displayed with expand/collapse behavior.

## Features

### Search
- Default search across all columns where `Searchable = true`
- Custom search predicates via `SearchPredicate`
- Debounced input with immediate apply on `Enter`
- Flexible search text extraction per column via `SearchTextSelector`

### Sorting
- Column sorting with ascending / descending toggle
- Sort indicators in the header
- Custom sort values via `SortValueSelector`
- Tree-aware sorting that only reorders siblings within the same parent level

### Column visibility
- Hideable columns via `Hideable`
- Hidden-column restore buttons below the table

### Hierarchical data
- Expand/collapse in `FancyTreeTable`
- Shared column model across flat and tree tables
- Search keeps matching nodes and their ancestors visible
- Configurable child lookup via `ChildItemsSelector`
- Optional `HasChildrenSelector` optimization
- Null child entries are ignored during tree construction

### Customization
- Custom toolbar via `ToolbarTemplate`
- Custom cell content via `CellTemplate`
- Full-row customization through a unified `RowTemplate`
- Conditional row replacement via `RowTemplateSelector`
- Unified row context for flat and tree tables via `FancyRowContext<TItem>`
- Optional row attributes via `RowAttributesSelector`
- Optional leading-content hook for the first cell via `RowLeadingContentTemplate`
- Row classes via `RowClassSelector`
- Header and cell styling via `HeaderClass` and `CellClass`

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Items` | `IReadOnlyList<TItem>` | The data items to display in the table (required) |
| `Columns` | `IReadOnlyList<FancyColumn<TItem>>` | Column configuration (required) |
| `SearchPlaceholder` | `string` | Placeholder text for the default search input (default: `"Search"`) |
| `ToolbarTemplate` | `RenderFragment?` | Custom toolbar content; replaces the default search bar |
| `SearchPredicate` | `Func<TItem, string, bool>?` | Custom search logic; overrides default column-based search |
| `RowClassSelector` | `Func<TItem, string?>?` | Returns CSS class(es) for each row |
| `RowAttributesSelector` | `Func<FancyRowContext<TItem>, IReadOnlyDictionary<string, object?>?>?` | Supplies additional attributes for the rendered `<tr>` |
| `RowLeadingContentTemplate` | `RenderFragment<FancyRowContext<TItem>>?` | Renders extra content at the start of the first visible cell |
| `RowTemplate` | `RenderFragment<FancyRowContext<TItem>>?` | Renders a complete custom table row (`<tr>...</tr>`) |
| `RowTemplateSelector` | `Func<FancyRowContext<TItem>, bool>?` | Chooses which rows use `RowTemplate`; if omitted, `RowTemplate` applies to every row |

## Tree Table Parameters

`FancyTreeTable<TItem>` supports the same parameters as `FancyTable<TItem>` and adds the following:

| Parameter | Type | Description |
|-----------|------|-------------|
| `ChildItemsSelector` | `Func<TItem, IEnumerable<TItem>?>` | Returns the child items for a given node (required) |
| `HasChildrenSelector` | `Func<TItem, bool>?` | Optional optimization to indicate whether a node should render an expand/collapse toggle |
| `ExpandLabel` | `string` | Accessible label for collapsed nodes (default: `"Expand"`) |
| `CollapseLabel` | `string` | Accessible label for expanded nodes (default: `"Collapse"`) |

## Row Context

`FancyRowContext<TItem>` is passed into `RowTemplate`, `RowTemplateSelector`, `RowAttributesSelector`, and `RowLeadingContentTemplate`.

| Property | Type | Description |
|----------|------|-------------|
| `Item` | `TItem` | The current row item |
| `Columns` | `IReadOnlyList<FancyColumn<TItem>>` | Currently visible columns |
| `VisibleColumnCount` | `int` | Count of visible columns, useful for `colspan` rows |
| `IsTreeNode` | `bool` | Indicates whether the row comes from `FancyTreeTable` |
| `Depth` | `int` | Tree depth for tree rows; `0` for flat tables |
| `HasChildren` | `bool` | Whether the tree row has children |
| `IsExpanded` | `bool` | Whether the tree row is currently expanded |
| `CanToggle` | `bool` | Convenience flag for toggle availability |
| `ToggleNode` | `EventCallback` | Expands or collapses the current tree node |
| `ExpandLabel` | `string` | Accessible label for collapsed nodes |
| `CollapseLabel` | `string` | Accessible label for expanded nodes |

For flat tables, tree-specific values are neutral: `IsTreeNode = false`, `Depth = 0`, `HasChildren = false`, and `ToggleNode` is empty.

## Column Configuration

Each column is configured using `FancyColumn<TItem>`:

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Unique identifier for the column |
| `Title` | `string` | Display name shown in the header |
| `Sortable` | `bool` | Whether the column can be sorted |
| `Searchable` | `bool` | Whether the column is included in search (default: `true`) |
| `Hideable` | `bool` | Whether the column can be hidden by the user |
| `HeaderClass` | `string?` | CSS class applied to the header cell |
| `CellClass` | `string?` | CSS class applied to data cells |
| `ValueSelector` | `Func<TItem, object?>?` | Extracts the value to display for each row |
| `SortValueSelector` | `Func<TItem, IComparable?>?` | Extracts the value used for sorting (falls back to `ValueSelector`) |
| `SearchTextSelector` | `Func<TItem, string?>?` | Extracts the text used for searching (falls back to `ValueSelector?.ToString()`) |
| `CellTemplate` | `RenderFragment<TItem>?` | Custom Blazor template to render cell content |

## Setup

To use `SoPro.FancyTable` in your Blazor application, include Bootstrap CSS and Bootstrap Icons in your app (`App.razor` / host page):

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.7/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
```

Add `SoPro.FancyTable.Components` to your `_Imports.razor` and rebuild the project:

```razor
@using SoPro.FancyTable.Components
```

## Usage Example

This example covers:
- default toolbar
- custom toolbar (`ToolbarTemplate`)
- custom cell template
- row-level classes (`RowClassSelector`)
- conditional custom rows (`RowTemplate` + `RowTemplateSelector`)
- custom row callbacks

```csharp
@page "/fancy-table-demo"

<div class="d-flex">
    <div class="p-3 col">
        <h5>Default Toolbar</h5>
        <FancyTable TItem="PersonRow"
                    Items="PeopleRows"
                    Columns="PeopleColumns"
                    SearchPlaceholder="Search name or city..." />
    </div>

    <div class="p-3 col">
        <h5>Custom Toolbar</h5>
        <FancyTable TItem="ProductRow"
                    Items="ProductRows"
                    Columns="ProductColumns">
            <ToolbarTemplate>
                <div class="d-flex">
                    <button class="btn btn-sm btn-info mx-1">
                        <i class="bi bi-search"></i>
                    </button>
                    <input class="form-control form-control-md" placeholder="Custom search UI" />
                </div>
            </ToolbarTemplate>
        </FancyTable>
    </div>

    <div class="p-3 col">
        <h5>Conditional custom rows</h5>
        <FancyTable TItem="PersonRow"
                    Items="PeopleRows"
                    Columns="StyledPeopleColumns"
                    RowClassSelector="GetPersonRowClass"
                    RowTemplate="PersonHighlightRow"
                    RowTemplateSelector="context => context.Item.Age >= 35" />
    </div>
</div>

@code {
    private IReadOnlyList<PersonRow> PeopleRows =
    [
        new("Alice", "Berlin", 31),
        new("Bob", "Hamburg", 27),
        new("Carol", "Munich", 36),
        new("David", "Cologne", 29)
    ];

    private IReadOnlyList<FancyColumn<PersonRow>> PeopleColumns =>
    [
        new FancyColumn<PersonRow>
        {
            Key = "name",
            Title = "Name",
            Sortable = true,
            Searchable = true,
            ValueSelector = person => person.Name,
            SortValueSelector = person => person.Name
        },
        new FancyColumn<PersonRow>
        {
            Key = "city",
            Title = "City",
            Sortable = true,
            Searchable = true,
            Hideable = true,
            ValueSelector = person => person.City,
            SortValueSelector = person => person.City
        },
        new FancyColumn<PersonRow>
        {
            Key = "age",
            Title = "Age",
            Sortable = true,
            Searchable = false,
            ValueSelector = person => person.Age,
            SortValueSelector = person => person.Age
        }
    ];

    private IReadOnlyList<ProductRow> ProductRows =
    [
        new("SW-1001", "Switch", 149.99m, 42, "NetWare Ltd"),
        new("FW-2300", "Firewall", 899.00m, 5, "SecureCore AG")
    ];

    private IReadOnlyList<FancyColumn<ProductRow>> ProductColumns =>
    [
        new FancyColumn<ProductRow>
        {
            Key = "sku",
            Title = "SKU",
            Sortable = true,
            Searchable = true,
            ValueSelector = product => product.Sku,
            SortValueSelector = product => product.Sku
        },
        new FancyColumn<ProductRow>
        {
            Key = "price",
            Title = "Price",
            Sortable = true,
            Searchable = false,
            ValueSelector = product => product.Price.ToString("C2"),
            SortValueSelector = product => product.Price
        }
    ];

    private IReadOnlyList<FancyColumn<PersonRow>> StyledPeopleColumns =>
    [
        new FancyColumn<PersonRow>
        {
            Key = "styled-name",
            Title = "Name",
            Sortable = true,
            Searchable = true,
            HeaderClass = "text-bg-dark",
            CellClass = "fw-semibold text-primary",
            ValueSelector = person => person.Name,
            SortValueSelector = person => person.Name
        },
        new FancyColumn<PersonRow>
        {
            Key = "styled-age",
            Title = "Age",
            Sortable = true,
            Searchable = false,
            CellTemplate = person => @<span class="badge text-bg-info-subtle border border-info text-info-emphasis">@person.Age yrs</span>
        }
    ];

    private string? GetPersonRowClass(PersonRow person) => person.Age >= 35 ? "bg-warning" : null;

    private RenderFragment<FancyRowContext<PersonRow>> PersonHighlightRow => context => @<tr class="table-warning">
        <td colspan="@context.VisibleColumnCount">
            <strong>@context.Item.Name</strong> is flagged for review from @context.Item.City.
            <button class="btn btn-sm btn-primary ms-2" @onclick="() => OpenPerson(context.Item)">Details</button>
        </td>
    </tr>;

    private void OpenPerson(PersonRow person)
    {
        Console.WriteLine($"Open details for {person.Name}");
    }

    private sealed record PersonRow(string Name, string City, int Age);
    private sealed record ProductRow(string Sku, string Category, decimal Price, int Stock, string Supplier);
}
```

`RowTemplate` renders the full row (`<tr>...</tr>`). If `RowTemplateSelector` is omitted, the custom row template applies to every row.

## Tree Table Example

`FancyTreeTable<TItem>` works with nested data while keeping the same column definition style:

```csharp
@page "/fancy-tree-table-demo"

<FancyTreeTable TItem="RuleRow"
                Items="Rules"
                Columns="RuleColumns"
                ChildItemsSelector="row => row.Children"
                HasChildrenSelector="row => row.Children.Count > 0"
                RowTemplate="SectionHeaderTemplate"
                RowTemplateSelector="context => context.Item.IsSectionHeader"
                SearchPlaceholder="Search rules..." />

@code {
    private static readonly RuleRow Rule_11_1 = new("11.1", "Allow HTTPS", "TCP", "443", "Any", "Server-A", "Allow", [], false);
    private static readonly RuleRow Rule_11_2 = new("11.2", "Allow DNS", "UDP", "53", "Any", "DNS-1", "Allow", [], false);
    private static readonly RuleRow Rule_11_3 = new("11.3", "Block Telnet", "TCP", "23", "Any", "Any", "Deny", [], false);

    private IReadOnlyList<RuleRow> Rules =
    [
        new("", "Section Header", "", "", "", "", "", [Rule_11_1, Rule_11_2, Rule_11_3], IsSectionHeader: true)
    ];

    private IReadOnlyList<FancyColumn<RuleRow>> RuleColumns =>
    [
        new() { Key = "number", Title = "Rule", ValueSelector = x => x.Number },
        new() { Key = "name", Title = "Name", ValueSelector = x => x.Name },
        new() { Key = "protocol", Title = "Protocol", ValueSelector = x => x.Protocol },
        new() { Key = "port", Title = "Port", ValueSelector = x => x.Port },
        new() { Key = "source", Title = "Source", ValueSelector = x => x.Source },
        new() { Key = "destination", Title = "Destination", ValueSelector = x => x.Destination },
        new() { Key = "action", Title = "Action", ValueSelector = x => x.Action }
    ];

    private RenderFragment<FancyRowContext<RuleRow>> SectionHeaderTemplate => context => @<tr class="table-secondary fw-bold">
        <td colspan="@context.VisibleColumnCount">
            <button type="button"
                    class="btn btn-link btn-sm text-decoration-none p-0 me-2"
                    @onclick="() => context.ToggleNode.InvokeAsync()"
                    aria-label="@(context.IsExpanded ? context.CollapseLabel : context.ExpandLabel)"
                    aria-expanded="@context.IsExpanded">
                <i class="bi @(context.IsExpanded ? "bi-caret-down-fill" : "bi-caret-right-fill")"></i>
            </button>
            @context.Item.Name
            <span class="ms-2 text-muted">(@context.Item.Children.Count Regeln)</span>
            <button class="btn btn-sm btn-outline-primary ms-3" @onclick="() => InspectSection(context.Item)">Inspect</button>
        </td>
    </tr>;

    private void InspectSection(RuleRow row)
    {
        Console.WriteLine($"Inspect section {row.Name}");
    }

    private sealed record RuleRow(
        string Number,
        string Name,
        string Protocol,
        string Port,
        string Source,
        string Destination,
        string Action,
        IReadOnlyList<RuleRow> Children,
        bool IsSectionHeader = false);
}
```

## Tree Search and Sorting Semantics

- Search shows matching nodes and their ancestors, so hits remain visible in context
- Clearing the search restores the manual expand/collapse state from before the search
- Sorting keeps the tree structure intact by sorting only within each sibling group

## Component Dependencies

- **Bootstrap 5**: For styling and grid utilities (MIT License)
- **Bootstrap Icons**: For UI icons (search, sort, eye, etc.) (MIT License)

## License

This project is licensed under the **MIT License**.

Bootstrap and Bootstrap Icons are also licensed under the MIT License.

---

## Roadmap

- [x] NuGet package release
- [ ] Pagination support (maybe with custom template)
- [ ] Localization support for default UI text (search placeholder, aria labels)
- [ ] Column resizing and reordering
- [ ] Export to CSV/Excel
- [ ] Row selection and bulk actions
- [ ] Dark Mode support
- [ ] Accessibility improvements (ARIA roles, keyboard navigation)
- [ ] Performance optimizations for large datasets (virtualization)
