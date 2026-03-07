# SoPro.FancyTable

A feature-rich, generic Blazor table component for displaying and interacting with collections of data.

## Overview

`FancyTable` is a reusable Blazor component that provides an interactive table experience with built-in support for searching, sorting, and column visibility management. It's designed to work with any data type through its generic `TItem` parameter.

## Features

### 🔍 Search
- **Default search**: Automatically searches across all searchable columns
- **Custom search predicates**: Define custom search logic via `SearchPredicate` parameter
- **Debounced input**: Optimized search input with 250ms debounce to prevent excessive filtering
- **Quick actions**: Search and clear buttons for manual control
- **Flexible search text**: Configure custom search text extraction per column via `SearchTextSelector`

### 📊 Sorting
- **Column sorting**: Click sortable column headers to sort ascending or descending
- **Visual indicators**: Icons show current sort state (bi-arrow-down-up, bi-sort-up, bi-sort-down)
- **Custom sort values**: Define custom sort comparisons per column via `SortValueSelector`
- **Toggle direction**: Click the same column header again to reverse sort order

### 👁️ Column Visibility
- **Hide columns**: Eye-slash button on column headers to hide columns on demand
- **Show hidden columns**: Dedicated section at the bottom displays hidden columns with restore buttons
- **Flexible configuration**: Each column can be marked as hideable via configuration

### 🎨 Customization
- **Custom toolbar**: Replace the default search bar with a custom toolbar template via `ToolbarTemplate`
- **Column templates**: Render custom content in cells via `CellTemplate`
- **Custom styling**: Apply custom CSS classes to headers and cells via `HeaderClass` and `CellClass`
- **Search placeholder**: Customize the search input placeholder text

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Items` | `IReadOnlyList<TItem>` | The data items to display in the table (required) |
| `Columns` | `IReadOnlyList<FancyColumn<TItem>>` | Column configuration (required) |
| `SearchPlaceholder` | `string` | Placeholder text for search input (default: "Search") |
| `ToolbarTemplate` | `RenderFragment?` | Custom toolbar content; replaces default search bar |
| `SearchPredicate` | `Func<TItem, string, bool>?` | Custom search logic; overrides default column-based search |

## Column Configuration

Each column is configured using `FancyColumn<TItem>`:

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Unique identifier for the column |
| `Title` | `string` | Display name shown in the header |
| `Sortable` | `bool` | Whether the column can be sorted |
| `Searchable` | `bool` | Whether the column is included in search (default: true) |
| `Hideable` | `bool` | Whether the column can be hidden by the user |
| `HeaderClass` | `string?` | CSS class applied to the header cell |
| `CellClass` | `string?` | CSS class applied to data cells |
| `ValueSelector` | `Func<TItem, object?>?` | Extracts the value to display for each row |
| `SortValueSelector` | `Func<TItem, IComparable?>?` | Extracts the value used for sorting (falls back to `ValueSelector`) |
| `SearchTextSelector` | `Func<TItem, string?>?` | Extracts the text used for searching (falls back to `ValueSelector?.ToString()`) |
| `CellTemplate` | `RenderFragment<TItem>?` | Custom Blazor template to render cell content |

## Usage Example

```csharp
@page "/fancy-table-demo"
@using YourNamespace.Components

<FancyTable TItem="Person" Items="people" Columns="columns" />

@code {
    private List<Person> people = new()
    {
        new Person { Id = 1, Name = "Alice", Email = "alice@example.com", Age = 30 },
        new Person { Id = 2, Name = "Bob", Email = "bob@example.com", Age = 25 },
    };

    private readonly List<FancyColumn<Person>> columns = new()
    {
        new FancyColumn<Person>
        {
            Key = "name",
            Title = "Name",
            Sortable = true,
            Hideable = true,
            ValueSelector = p => p.Name,
        },
        new FancyColumn<Person>
        {
            Key = "email",
            Title = "Email",
            Sortable = true,
            ValueSelector = p => p.Email,
        },
        new FancyColumn<Person>
        {
            Key = "age",
            Title = "Age",
            Sortable = true,
            Hideable = true,
            ValueSelector = p => p.Age,
            SortValueSelector = p => p.Age, // Sort numerically
        },
    };

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
    }
}
```

## Component Dependencies

- **Bootstrap 5**: For styling and grid utilities (MIT License)
- **Bootstrap Icons**: For UI icons (search, sort, eye, etc.) (MIT License)

## License

This project is licensed under the **MIT License** - see the LICENSE file for details.

Bootstrap and Bootstrap Icons are also licensed under the MIT License, which is one of the most permissive open-source licenses. This allows for unrestricted use, modification, and distribution of this project with minimal restrictions.