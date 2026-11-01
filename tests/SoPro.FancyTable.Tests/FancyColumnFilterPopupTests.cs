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
        Action? onClear = null)
    {
        return Render<FancyColumnFilterPopup>(parameters => parameters
            .Add(p => p.ColumnKey, "name")
            .Add(p => p.ColumnTitle, "Name")
            .Add(p => p.ActiveFilter, active)
            .Add(p => p.Texts, FancyTableTexts.Default)
            .Add(p => p.OnApply, EventCallback.Factory.Create<FancyColumnFilter>(this, f => onApply?.Invoke(f)))
            .Add(p => p.OnClear, EventCallback.Factory.Create(this, () => onClear?.Invoke())));
    }

    private static void Open(IRenderedComponent<FancyColumnFilterPopup> cut)
    {
        cut.Find("button.fancy-filter-button").Click();
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
        cut.Find("select").Change("Equals");
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
}
