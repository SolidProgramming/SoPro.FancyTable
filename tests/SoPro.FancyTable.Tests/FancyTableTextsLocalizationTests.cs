using System.Globalization;
using System.Reflection;
using System.Resources;

[CollectionDefinition("Culture", DisableParallelization = true)]
public sealed class CultureCollection
{
}

[Collection("Culture")]
public sealed class FancyTableTextsLocalizationTests
{
    private static readonly CultureInfo German = new("de");

    private static IEnumerable<PropertyInfo> TextProperties =>
        typeof(FancyTableTexts)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static property => property.PropertyType == typeof(string));

    [Fact]
    public void ForCulture_German_ReturnsGermanTexts()
    {
        FancyTableTexts texts = FancyTableTexts.ForCulture(German);

        Assert.Equal("Anwenden", texts.ApplyFilterLabel);
        Assert.Equal("Ist", texts.FilterOperatorEqualsLabel);
        Assert.Equal("Suchen", texts.SearchPlaceholder);
        Assert.Equal("Zurück", texts.PreviousPageLabel);
    }

    [Fact]
    public void ForCulture_UnsupportedCulture_FallsBackToEnglish()
    {
        FancyTableTexts texts = FancyTableTexts.ForCulture(new CultureInfo("fr"));

        foreach (PropertyInfo property in TextProperties)
        {
            Assert.Equal(property.GetValue(FancyTableTexts.Default), property.GetValue(texts));
        }
    }

    [Fact]
    public void ForCulture_RegionalCulture_UsesParent()
    {
        FancyTableTexts texts = FancyTableTexts.ForCulture(new CultureInfo("de-AT"));

        Assert.Equal("Anwenden", texts.ApplyFilterLabel);
    }

    [Fact]
    public void ForCulture_SameCulture_ReturnsCachedInstance()
    {
        Assert.Same(FancyTableTexts.ForCulture(German), FancyTableTexts.ForCulture(new CultureInfo("de")));
    }

    [Fact]
    public void ForCulture_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FancyTableTexts.ForCulture(null!));
    }

    [Fact]
    public void Current_FollowsCurrentUICulture()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = German;
            Assert.Equal("Anwenden", FancyTableTexts.Current.ApplyFilterLabel);

            CultureInfo.CurrentUICulture = new CultureInfo("en");
            Assert.Equal("Apply", FancyTableTexts.Current.ApplyFilterLabel);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void ResourceKeys_MatchPropertiesInAllCultures()
    {
        ResourceManager resources = new("SoPro.FancyTable.FancyTable", typeof(FancyTableTexts).Assembly);

        foreach (PropertyInfo property in TextProperties)
        {
            string? english = (string?)property.GetValue(FancyTableTexts.Default);

            Assert.Equal(english, resources.GetString(property.Name, CultureInfo.InvariantCulture));
            Assert.Equal(english, resources.GetString(property.Name, new CultureInfo("en")));
            Assert.False(string.IsNullOrEmpty(resources.GetString(property.Name, German)), $"Missing German text for {property.Name}");
        }

        Assert.NotEqual(FancyTableTexts.Default.ApplyFilterLabel, resources.GetString(nameof(FancyTableTexts.ApplyFilterLabel), German));
    }
}
