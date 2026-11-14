using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace ReactionLab.Notifications.Mail;

public sealed class MailRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
{
    public async Task<RenderedMail> RenderAsync(MailContent content)
    {
        await using var renderer = new HtmlRenderer(services, loggerFactory);

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(MailDocument.Content)] = content });
            var document = await renderer.RenderComponentAsync<MailDocument>(parameters);

            return document.ToHtmlString();
        });

        return new RenderedMail(content.Subject, html, RenderText(content));
    }

    private static string RenderText(MailContent content)
    {
        var text = new StringBuilder()
            .Append(MailStyle.BrandName).Append("\n\n")
            .Append(content.Heading).Append('\n');

        foreach (var block in content.Blocks.Where(block => block is not MailLinkCopy))
        {
            text.Append('\n');

            switch (block)
            {
                case MailButton button:
                    text.Append(button.Label).Append(": ").Append(button.Target.AbsoluteUri).Append('\n');
                    break;
                case MailParagraph paragraph:
                    text.Append(paragraph.Text).Append('\n');
                    break;
                case MailFacts facts:
                    foreach (var fact in facts.Facts)
                    {
                        text.Append(fact.Label).Append(": ").Append(fact.Value).Append('\n');
                    }

                    break;
            }
        }

        text.Append("\n--\n");

        foreach (var line in content.FooterLines)
        {
            text.Append(line).Append('\n');
        }

        return text.ToString().ReplaceLineEndings("\n");
    }
}
