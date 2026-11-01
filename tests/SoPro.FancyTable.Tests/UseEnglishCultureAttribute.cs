using System.Globalization;
using System.Reflection;
using Xunit.Sdk;

[assembly: UseEnglishCulture]

// Built-in texts follow the UI culture, so every test runs with a fixed culture
// unless it switches explicitly. Otherwise results depend on the machine's language.
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UseEnglishCultureAttribute : BeforeAfterTestAttribute
{
    private static readonly CultureInfo TestCulture = new("en-US");

    private CultureInfo? originalCulture;
    private CultureInfo? originalUICulture;

    public override void Before(MethodInfo methodUnderTest)
    {
        originalCulture = CultureInfo.CurrentCulture;
        originalUICulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = TestCulture;
        CultureInfo.CurrentUICulture = TestCulture;
    }

    public override void After(MethodInfo methodUnderTest)
    {
        if (originalCulture is not null)
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        if (originalUICulture is not null)
        {
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }
}
