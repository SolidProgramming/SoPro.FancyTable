# SoPro.FancyTable

https://www.nuget.org/packages/SoPro.FancyTable

![Playground.png](https://github.com/SolidProgramming/SoPro.FancyTable/blob/ba84b575fafa1c86a7c4e7f074de32cb52be5c18/Playground.png)

## Overview

`FancyTable` is a reusable Blazor component that provides an interactive table experience with built-in support for searching, sorting, and column visibility management. It's designed to work with any data type through its generic `TItem` parameter.

`FancyTreeTable` extends the same column model and interaction patterns to hierarchical datasets, so parent/child structures can be displayed with expand/collapse behavior.

## Features

### 🔍 Search
- **Default search**: Automatically searches across all columns where `Searchable = true`
- **Custom search predicates**: Define custom search logic via `SearchPredicate`
- **Debounced input**: Search input applies with a `250ms` debounce
- **Immediate search on Enter**: Press `Enter` to apply the current search text immediately
- **Quick actions**: Search/clear button behavior in the default toolbar
- **Flexible search text**: Configure custom search text extraction per column via `SearchTextSelector`

### 📊 Sorting
- **Column sorting**: Click sortable column headers to sort ascending or descending
- **Visual indicators**: Icons show current sort state (`bi-arrow-down-up`, `bi-sort-up`, `bi-sort-down`)
- **Custom sort values**: Define custom sort comparisons per column via `SortValueSelector`
- **Fallback sort value**: Falls back to `ValueSelector` when `SortValueSelector` is not set
- **Toggle direction**: Click the same column header again to reverse sort order
- **Tree-aware sorting**: In `FancyTreeTable`, sorting reorders only siblings within the same parent level

### 👁️ Column Visibility
- **Hide columns**: Eye-slash button on hideable column headers hides columns on demand
- **Show hidden columns**: Dedicated section below the table displays hidden columns with restore buttons
- **Flexible configuration**: Each column can be marked as hideable via `Hideable`

### 🌳 Hierarchical Data
- **Expand/collapse**: `FancyTreeTable` renders nested rows with toggles in the first visible column
- **Same column model**: Reuse `FancyColumn<TItem>` definitions across flat and tree tables
- **Context-aware search**: Search results keep matching nodes and their ancestors visible
- **Configurable child lookup**: Supply nested data via `ChildItemsSelector` and optionally `HasChildrenSelector`
- **Defensive null handling**: `null` child entries returned from consumer data are ignored during tree construction

### 🎨 Customization
- **Custom toolbar**: Replace the default search bar via `ToolbarTemplate`
- **Column templates**: Render custom cell content via `CellTemplate`
- **Conditional row templates**: Replace the built-in row markup via `RowTemplate` when `RowTemplateSelector` matches
- **Tree-specific row templates**: Use `TreeRowTemplate` when custom tree rows need node state or the toggle callback
- **Header and cell styling**: Apply CSS classes via `HeaderClass` and `CellClass`
- **Row styling**: Apply row CSS classes via `RowClassSelector`
- **Search placeholder**: Customize the default search input placeholder via `SearchPlaceholder`

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Items` | `IReadOnlyList<TItem>` | The data items to display in the table (required) |
| `Columns` | `IReadOnlyList<FancyColumn<TItem>>` | Column configuration (required) |
| `SearchPlaceholder` | `string` | Placeholder text for the default search input (default: `"Search"`) |
| `ToolbarTemplate` | `RenderFragment?` | Custom toolbar content; replaces the default search bar |
| `SearchPredicate` | `Func<TItem, string, bool>?` | Custom search logic; overrides default column-based search |
| `RowClassSelector` | `Func<TItem, string?>?` | Returns CSS class(es) for each row |
| `RowTemplate` | `RenderFragment<TItem>?` | Renders a complete custom table row (`<tr>...</tr>`) for matching items |
| `RowTemplateSelector` | `Func<TItem, bool>?` | Chooses which items use `RowTemplate`; rows fall back to the built-in rendering when it returns `false` |

## Tree Table Parameters

`FancyTreeTable<TItem>` supports the same parameters as `FancyTable<TItem>` and adds the following:

| Parameter | Type | Description |
|-----------|------|-------------|
| `ChildItemsSelector` | `Func<TItem, IEnumerable<TItem>?>` | Returns the child items for a given node (required) |
| `HasChildrenSelector` | `Func<TItem, bool>?` | Optional optimization to indicate whether a node should render an expand/collapse toggle |
| `ExpandLabel` | `string` | Accessible label for collapsed nodes (default: `"Expand"`) |
| `CollapseLabel` | `string` | Accessible label for expanded nodes (default: `"Collapse"`) |
| `TreeRowTemplate` | `RenderFragment<FancyTreeRowTemplateContext<TItem>>?` | Renders a complete custom tree row with access to node state and toggle callback |
| `TreeRowTemplateSelector` | `Func<TreeNodeState<TItem>, bool>?` | Chooses which nodes use `TreeRowTemplate`; checked before `RowTemplate` |

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
- custom header/cell classes
- custom cell template
- row-level classes (`RowClassSelector`)
- conditional custom rows (`RowTemplate` + `RowTemplateSelector`)

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
        <h5>Custom Toolbar (via ToolbarTemplate)</h5>
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
        <h5>Custom Table header + row css classes & CellTemplates</h5>
        <FancyTable TItem="PersonRow"
                    Items="PeopleRows"
                    Columns="StyledPeopleColumns"
                    RowClassSelector="GetPersonRowClass"
                    RowTemplate="PersonHighlightRow"
                    RowTemplateSelector="ShouldRenderPersonHighlightRow" />
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
        new("FW-2300", "Firewall", 899.00m, 5, "SecureCore AG"),
        new("AP-550", "Access Point", 219.50m, 18, "WaveLink"),
        new("RTR-910", "Router", 399.00m, 9, "RouteStack Inc")
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
            SortValueSelector = product => product.Sku,
        },
        new FancyColumn<ProductRow>
        {
            Key = "category",
            Title = "Category",
            Sortable = true,
            Searchable = true,
            Hideable = true,
            ValueSelector = product => product.Category,
            SortValueSelector = product => product.Category
        },
        new FancyColumn<ProductRow>
        {
            Key = "price",
            Title = "Price",
            Sortable = true,
            Searchable = false,
            ValueSelector = product => product.Price.ToString("C2"),
            SortValueSelector = product => product.Price
        },
        new FancyColumn<ProductRow>
        {
            Key = "stock",
            Title = "In Stock",
            Sortable = true,
            Searchable = false,
            ValueSelector = product => product.Stock,
            SortValueSelector = product => product.Stock
        },
        new FancyColumn<ProductRow>
        {
            Key = "supplier",
            Title = "Supplier",
            Sortable = true,
            Searchable = true,
            Hideable = true,
            ValueSelector = product => product.Supplier,
            SortValueSelector = product => product.Supplier
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
            Key = "styled-city",
            Title = "City",
            Sortable = true,
            Searchable = true,
            HeaderClass = "text-bg-secondary",
            CellClass = "fst-italic",
            ValueSelector = person => person.City,
            SortValueSelector = person => person.City
        },
        new FancyColumn<PersonRow>
        {
            Key = "styled-age",
            Title = "Age",
            Sortable = true,
            Searchable = false,
            Hideable = true,
            HeaderClass = "text-bg-info",
            CellClass = "text-center",
            SortValueSelector = person => person.Age,
            CellTemplate = person => @<span class="badge text-bg-info-subtle border border-info text-info-emphasis">@person.Age yrs</span>
        }
    ];

    private string? GetPersonRowClass(PersonRow person) => person.Age >= 35 ? "bg-warning" : null;

    private bool ShouldRenderPersonHighlightRow(PersonRow person) => person.Age >= 35;

    private RenderFragment<PersonRow> PersonHighlightRow => person => @<tr class="table-warning">
        <td colspan="3">
            <strong>@person.Name</strong> is flagged for review from @person.City and is currently @person.Age years old.
        </td>
    </tr>;

    private sealed record PersonRow(string Name, string City, int Age);
    private sealed record ProductRow(string Sku, string Category, decimal Price, int Stock, string Supplier);
}
```

`RowTemplate` must render the full row (`<tr>...</tr>`). If `RowTemplateSelector` is not set, or returns `false`, the component uses the normal column-based row rendering.

## Tree Table Example

`FancyTreeTable<TItem>` works with nested data while keeping the same column definition style:

```csharp
@page "/fancy-tree-table-demo"

<FancyTreeTable TItem="FolderNode"
                Items="Folders"
                Columns="FolderColumns"
                ChildItemsSelector="node => node.Children"
                HasChildrenSelector="node => node.Children.Count > 0"
                SearchPlaceholder="Search folders or files..." />

@code {
    private IReadOnlyList<FolderNode> Folders =
    [
        new("Projects", "Folder", 0, true,
        [
            new("SoPro.FancyTable", "Folder", 0, true,
            [
                new("README.md", "File", 12_288, false, []),
                new("Components", "Folder", 0, true,
                [
                    new("FancyTreeTable.razor", "File", 10_240, false, [])
                ])
            ]),
            new("Archive", "Folder", 0, true, [])
        ]),
        new("Downloads", "Folder", 0, true, [])
    ];

    private IReadOnlyList<FancyColumn<FolderNode>> FolderColumns =>
    [
        new FancyColumn<FolderNode>
        {
            Key = "name",
            Title = "Name",
            Sortable = true,
            Searchable = true,
            ValueSelector = node => node.Name,
            SortValueSelector = node => node.Name
        },
        new FancyColumn<FolderNode>
        {
            Key = "kind",
            Title = "Type",
            Sortable = true,
            Searchable = true,
            ValueSelector = node => node.Kind,
            SortValueSelector = node => node.Kind
        },
        new FancyColumn<FolderNode>
        {
            Key = "size",
            Title = "Size",
            Sortable = true,
            Searchable = false,
            ValueSelector = node => node.IsFolder ? "-" : $"{node.SizeInBytes:N0} B",
            SortValueSelector = node => node.SizeInBytes
        }
    ];

    private sealed record FolderNode(
        string Name,
        string Kind,
        long SizeInBytes,
        bool IsFolder,
        IReadOnlyList<FolderNode> Children);
}
```

`FancyTreeTable<TItem>` supports the same `RowTemplate` and `RowTemplateSelector` parameters. When you also need access to expand/collapse state or the actual toggle callback, use `TreeRowTemplate` with `TreeRowTemplateSelector` instead. `TreeRowTemplate` is evaluated first and receives a `FancyTreeRowTemplateContext<TItem>` containing `Node`, `ToggleNode`, `ExpandLabel`, and `CollapseLabel`. `ChildItemsSelector` may also return `null`, and any `null` child entries are ignored.

Example for a collapsible section header row:

```csharp
<FancyTreeTable TItem="RuleRow"
                Items="Rules"
                Columns="RuleColumns"
                ChildItemsSelector="row => row.Children"
                HasChildrenSelector="row => row.Children.Count > 0"
                TreeRowTemplate="SectionHeaderTemplate"
                TreeRowTemplateSelector="node => node.Item.IsSectionHeader" />

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

    private RenderFragment<FancyTreeRowTemplateContext<RuleRow>> SectionHeaderTemplate => context => @<tr class="table-secondary fw-bold">
        <td colspan="7">
            <button type="button"
                    class="btn btn-link btn-sm text-decoration-none p-0 me-2"
                    @onclick="() => context.ToggleNode.InvokeAsync(context.Node)"
                    aria-label="@(context.Node.IsExpanded ? context.CollapseLabel : context.ExpandLabel)"
                    aria-expanded="@context.Node.IsExpanded">
                <i class="bi @(context.Node.IsExpanded ? "bi-caret-down-fill" : "bi-caret-right-fill")"></i>
            </button>
            @context.Node.Item.Name
            <span class="ms-2 text-muted">(@context.Node.Children.Count Regeln)</span>
        </td>
    </tr>;

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

### Tree Search and Sorting Semantics

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

## 🏁 Roadmap

- [x] NuGet package release 📦
- [ ] Pagination support (maybe with custom template) ⏭️
- [ ] Localization support for default UI text (search placeholder, aria labels) 🌐
- [ ] Column resizing and reordering 📏
- [ ] Export to CSV/Excel 🖺
- [ ] Row selection and bulk actions ✨
- [ ] Dark Mode support 🌙
- [ ] Accessibility improvements (ARIA roles, keyboard navigation) ♿
- [ ] Performance optimizations for large datasets (virtualization) ⚡
