using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Substances;
using ReactionLab.Infrastructure.Persistence.Documents;
using Shouldly;
using Xunit;

namespace ReactionLab.Infrastructure.UnitTests.Documents;

public sealed class TranslationDocumentTests
{
    private static readonly SubstanceContent Original =
        SubstanceContent.Create("Test_Original", "Test_Original", "Test_Original").Value;

    [Fact]
    public void Serialize_WritesOnlyWhatEachLocaleTranslated()
    {
        var json = SubstanceContentDocument.Serialize(Partial());

        var translated = json.Split("\"uk\":", StringSplitOptions.None)[1];
        translated.ShouldNotContain("Test_Original");
    }

    [Fact]
    public void RoundTrip_PreservesTheUntranslatedGap()
    {
        var restored = SubstanceContentDocument.Deserialize(SubstanceContentDocument.Serialize(Partial()));

        restored.Stored(SupportedLocale.Ukrainian)!.IupacName.ShouldBeNull();
        restored.Resolve(SupportedLocale.Ukrainian).IupacName.ShouldBe("Test_Original");
        restored.Resolve(SupportedLocale.Ukrainian).Name.ShouldBe("Test_Translated");
    }

    [Fact]
    public void Deserialize_RejectsAnObjectWithNoDefaultLocale() =>
        Should.Throw<InvalidOperationException>(
            () => SubstanceContentDocument.Deserialize("""{"uk":{"name":"Test_Translated"}}"""));

    [Fact]
    public void Deserialize_SkipsALocaleThatIsNotLongerSupported() =>
        SubstanceContentDocument.Deserialize("""{"en":{"name":"Test_Supported"}, "not_supported":{"name":"test"}}""")
            .Locales.ShouldBe([SupportedLocale.English]);

    private static Translations<SubstanceContent> Partial() =>
        Translations.Create(Original).Value.With(SupportedLocale.Ukrainian, SubstanceContent.Create("Test_Translated").Value);
}
