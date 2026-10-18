using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class UnicodeText
{
    public static bool TryNormalize(string text, [NotNullWhen(true)] out string? normalized)
    {
        try
        {
            normalized = text.Normalize(NormalizationForm.FormC);

            return true;
        }
        catch (ArgumentException)
        {
            normalized = null;

            return false;
        }
    }
}
