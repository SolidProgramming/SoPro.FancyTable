using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SoPro.FancyTable.Components;
using Xunit;

public sealed class FancyColumnFilterPopupTests : BunitContext
{
    private const string ModulePath = "./_content/SoPro.FancyTable/fancy-table-filter.js";

    public FancyColumnFilterPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<FancyColumnFilterPopup> RenderPopup(
        FancyColumnFilter? active = null,
        Action<FancyColumnFilter>? onApply = null,
        Action? onClear = null,
        IReadOnlyList<FancyColumnFilterOperator>? allowedOperators = null,
        FancyColumnFilterType filterType = FancyColumnFilterType.Text)
    {
        return Render<FancyColumnFilterPopup>(parameters => parameters
            .Add(p => p.ColumnKey, "name")
            .Add(p => p.ColumnTitle, "Name")
            .Add(p => p.ActiveFilter, active)
            .Add(p => p.AllowedOperators, allowedOperators)
            .Add(p => p.FilterType, filterType)
            .Add(p => p.Texts, FancyTableTexts.Default)
            .Add(p => p.OnApply, EventCallback.Factory.Create<FancyColumnFilter>(this, f => onApply?.Invoke(f)))
            .Add(p => p.OnClear, EventCallback.Factory.Create(this, () => onClear?.Invoke())));
    }

    private static void Open(IRenderedComponent<FancyColumnFilterPopup> cut)
    {
        cut.Find("button.fancy-filter-button").Click();
    }

    private static List<string?> TextOperatorNames =>
        FancyColumnFilterExtensions.OperatorsFor(FancyColumnFilterType.Text).Select(op => (string?)op.ToString()).ToList();

    private static List<string?> OptionValues(IRenderedComponent<FancyColumnFilterPopup> cut)
    {
        return cut.FindAll("select option").Select(option => option.GetAttribute("value")).ToList();
    }

    [Fact]
    public void Open_WithoutAllowedOperators_OffersAllTextOperators()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup();
        Open(cut);

        Assert.Equal(TextOperatorNames, OptionValues(cut));
    }

    [Fact]
    public void Open_WithEmptyAllowedOperators_OffersAllTextOperators()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(allowedOperators: []);
        Open(cut);

        Assert.Equal(TextOperatorNames, OptionValues(cut));
    }

    [Fact]
    public void Open_WithAllowedOperators_OffersOnlyThoseInGivenOrderWithoutDuplicates()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(allowedOperators:
        [
            FancyColumnFilterOperator.NotEquals,
            FancyColumnFilterOperator.Equals,
            FancyColumnFilterOperator.NotEquals,
            (FancyColumnFilterOperator)99
        ]);
        Open(cut);

        Assert.Equal(["NotEquals", "Equals"], OptionValues(cut));
    }

    [Fact]
    public void Open_ContainsNotAllowed_PreselectsFirstAllowedOperator()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(allowedOperators: [FancyColumnFilterOperator.Equals, FancyColumnFilterOperator.NotEquals]);
        Open(cut);

        Assert.Equal("Equals", cut.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void Open_ContainsAllowed_PreselectsContains()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(allowedOperators: [FancyColumnFilterOperator.Equals, FancyColumnFilterOperator.Contains]);
        Open(cut);

        Assert.Equal("Contains", cut.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void Open_ActiveFilterOperatorNotAllowed_PreselectsDefaultOperator()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(
            new FancyColumnFilter("name", FancyColumnFilterOperator.StartsWith, "An"),
            allowedOperators: [FancyColumnFilterOperator.Equals]);
        Open(cut);

        Assert.Equal("Equals", cut.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void Apply_OperatorNotAllowed_IsNotRaised()
    {
        FancyColumnFilter? received = null;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(onApply: f => received = f, allowedOperators: [FancyColumnFilterOperator.Equals]);
        Open(cut);

        cut.Find("select").Change("StartsWith");
        cut.Find("input").Input("An");

        Assert.True(cut.Find("button.btn-primary").HasAttribute("disabled"));
        cut.Find("input").KeyDown("Enter");
        Assert.Null(received);
    }

    [Fact]
    public void RendersFunnelIcon_InactiveAndActive()
    {
        IRenderedComponent<FancyColumnFilterPopup> inactive = RenderPopup();
        Assert.NotNull(inactive.Find("i.bi-funnel"));
        Assert.Empty(inactive.FindAll("i.bi-funnel-fill"));
        Assert.Equal("Filter column Name", inactive.Find("button.fancy-filter-button").GetAttribute("aria-label"));

        IRenderedComponent<FancyColumnFilterPopup> active = RenderPopup(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "Anna"));
        Assert.NotNull(active.Find("i.bi-funnel-fill"));
    }

    [Fact]
    public void Open_PrefillsActiveFilter()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "Anna"));
        Open(cut);

        Assert.Equal("Equals", cut.Find("select").GetAttribute("value"));
        Assert.Equal("Anna", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Open_DefaultsToContainsAndEmpty()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup();
        Open(cut);

        Assert.Equal("Contains", cut.Find("select").GetAttribute("value"));
        Assert.True(string.IsNullOrEmpty(cut.Find("input").GetAttribute("value")));
        Assert.True(cut.Find("button.btn-primary").HasAttribute("disabled"));
    }

    [Fact]
    public void Apply_RaisesOnApplyWithDraft()
    {
        FancyColumnFilter? received = null;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(onApply: f => received = f);
        Open(cut);

        cut.Find("select").Change("StartsWith");
        cut.Find("input").Input("An");
        cut.Find("button.btn-primary").Click();

        Assert.Equal(new FancyColumnFilter("name", FancyColumnFilterOperator.StartsWith, "An"), received);
    }

    [Fact]
    public void EnterInValue_Applies()
    {
        int calls = 0;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(onApply: _ => calls++);
        Open(cut);

        cut.Find("input").Input("An");
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(1, calls);
    }

    [Fact]
    public void EnterInValue_WithEmptyValue_DoesNotApply()
    {
        int calls = 0;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(onApply: _ => calls++);
        Open(cut);

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(0, calls);
    }

    [Fact]
    public void EmptyOperator_HidesValueAndEnablesApply()
    {
        FancyColumnFilter? received = null;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(onApply: f => received = f);
        Open(cut);

        cut.Find("input").Input("stale");
        cut.Find("select").Change("IsEmpty");

        Assert.Empty(cut.FindAll("input"));
        Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled"));

        cut.Find("button.btn-primary").Click();
        Assert.Equal(new FancyColumnFilter("name", FancyColumnFilterOperator.IsEmpty, null), received);
    }

    [Fact]
    public void Clear_DisabledWithoutActiveFilter_RaisesOnClearOtherwise()
    {
        IRenderedComponent<FancyColumnFilterPopup> none = RenderPopup();
        Open(none);
        Assert.True(none.Find("button.btn-outline-secondary").HasAttribute("disabled"));

        int cleared = 0;
        IRenderedComponent<FancyColumnFilterPopup> active = RenderPopup(
            new FancyColumnFilter("name", FancyColumnFilterOperator.Contains, "x"),
            onClear: () => cleared++);
        Open(active);
        Assert.False(active.Find("button.btn-outline-secondary").HasAttribute("disabled"));
        active.Find("button.btn-outline-secondary").Click();

        Assert.Equal(1, cleared);
    }

    [Fact]
    public async Task Reopen_AfterClose_DiscardsDraft()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "Anna"));
        Open(cut);
        cut.Find("input").Input("xyz");

        await cut.InvokeAsync(() => cut.Instance.OnPopoverClosed());
        Open(cut);

        Assert.Equal("Anna", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Open_CallsJsShow()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup();

        Open(cut);

        module.VerifyInvoke("show");
    }

    [Fact]
    public async Task Reopen_ShowsAfterRenderWithResetDraft()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "Anna"));

        Open(cut);
        cut.Find("select").Change("IsEmpty");
        Assert.Empty(cut.FindAll("input"));
        await cut.InvokeAsync(() => cut.Instance.OnPopoverClosed());
        Open(cut);

        Assert.Single(cut.FindAll("input"));
        Assert.Equal(2, module.Invocations["show"].Count);
    }

    [Fact]
    public void Button_IsNativeInvokerForPopover()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup();

        string target = cut.Find("button.fancy-filter-button").GetAttribute("popovertarget")!;

        Assert.False(string.IsNullOrEmpty(target));
        Assert.Equal(target, cut.Find("div[popover]").GetAttribute("id"));
    }

    [Fact]
    public void ClickWhileOpen_DoesNotShowAgainNorResetDraft()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(new FancyColumnFilter("name", FancyColumnFilterOperator.Equals, "Anna"));

        Open(cut);
        cut.Find("input").Input("xyz");
        Open(cut);

        Assert.Single(module.Invocations["show"]);
        Assert.Equal("true", cut.Find("button.fancy-filter-button").GetAttribute("aria-expanded"));
        Assert.Equal("xyz", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task OnPopoverClosed_ResetsAriaExpanded()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup();

        Open(cut);
        Assert.Equal("true", cut.Find("button.fancy-filter-button").GetAttribute("aria-expanded"));
        await cut.InvokeAsync(() => cut.Instance.OnPopoverClosed());

        Assert.Equal("false", cut.Find("button.fancy-filter-button").GetAttribute("aria-expanded"));
    }

    [Theory]
    [InlineData(FancyColumnFilterType.Text, "text")]
    [InlineData(FancyColumnFilterType.Date, "date")]
    [InlineData(FancyColumnFilterType.Number, "number")]
    public void ValueInput_TypeFollowsFilterType(FancyColumnFilterType filterType, string inputType)
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(filterType: filterType);
        Open(cut);

        Assert.Equal(inputType, cut.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void NumberInput_AllowsDecimals()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(filterType: FancyColumnFilterType.Number);
        Open(cut);

        Assert.Equal("any", cut.Find("input").GetAttribute("step"));
    }

    [Fact]
    public void Open_DateColumn_OffersComparisonOperators()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(filterType: FancyColumnFilterType.Date);
        Open(cut);

        Assert.Equal(
            FancyColumnFilterExtensions.OperatorsFor(FancyColumnFilterType.Date).Select(op => (string?)op.ToString()).ToList(),
            OptionValues(cut));
        Assert.Equal("Equals", cut.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void Open_DateColumn_LabelsComparisonOperators()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(filterType: FancyColumnFilterType.Date);
        Open(cut);

        List<string> labels = cut.FindAll("select option").Select(option => option.TextContent).ToList();
        Assert.Contains("Is greater than", labels);
        Assert.Contains("Is greater than or equal to", labels);
        Assert.Contains("Is less than", labels);
        Assert.Contains("Is less than or equal to", labels);
        Assert.Contains("Is between", labels);
    }

    [Fact]
    public void Between_RendersFromAndToInputs()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(filterType: FancyColumnFilterType.Date);
        Open(cut);

        cut.Find("select").Change("Between");

        IReadOnlyList<AngleSharp.Dom.IElement> inputs = cut.FindAll("input");
        Assert.Equal(["From", "To"], inputs.Select(input => input.GetAttribute("aria-label")));
        Assert.All(inputs, input => Assert.Equal("date", input.GetAttribute("type")));
    }

    [Fact]
    public void Between_ApplyDisabledUntilBothFilled()
    {
        FancyColumnFilter? received = null;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(onApply: f => received = f, filterType: FancyColumnFilterType.Date);
        Open(cut);
        cut.Find("select").Change("Between");

        cut.Find("input[aria-label='From']").Input("2026-01-01");
        Assert.True(cut.Find("button.btn-primary").HasAttribute("disabled"));

        cut.Find("input[aria-label='To']").Input("2026-02-01");
        Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled"));

        cut.Find("button.btn-primary").Click();
        Assert.Equal(new FancyColumnFilter("name", FancyColumnFilterOperator.Between, "2026-01-01", "2026-02-01"), received);
    }

    [Fact]
    public void Between_EnterInToInput_Applies()
    {
        int calls = 0;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(onApply: _ => calls++, filterType: FancyColumnFilterType.Number);
        Open(cut);
        cut.Find("select").Change("Between");
        cut.Find("input[aria-label='From']").Input("1");
        cut.Find("input[aria-label='To']").Input("5");

        cut.Find("input[aria-label='To']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Open_ActiveBetween_PrefillsBothInputs()
    {
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(
            new FancyColumnFilter("name", FancyColumnFilterOperator.Between, "2026-01-01", "2026-02-01"),
            filterType: FancyColumnFilterType.Date);
        Open(cut);

        Assert.Equal("2026-01-01", cut.Find("input[aria-label='From']").GetAttribute("value"));
        Assert.Equal("2026-02-01", cut.Find("input[aria-label='To']").GetAttribute("value"));
    }

    [Fact]
    public void SwitchFromBetweenToGreaterThan_KeepsFromDropsTo()
    {
        FancyColumnFilter? received = null;
        IRenderedComponent<FancyColumnFilterPopup> cut = RenderPopup(onApply: f => received = f, filterType: FancyColumnFilterType.Date);
        Open(cut);
        cut.Find("select").Change("Between");
        cut.Find("input[aria-label='From']").Input("2026-01-01");
        cut.Find("input[aria-label='To']").Input("2026-02-01");

        cut.Find("select").Change("GreaterThan");
        Assert.Equal("2026-01-01", cut.Find("input").GetAttribute("value"));
        cut.Find("button.btn-primary").Click();

        Assert.Equal(new FancyColumnFilter("name", FancyColumnFilterOperator.GreaterThan, "2026-01-01"), received);
    }
}
