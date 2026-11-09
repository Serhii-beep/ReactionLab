namespace ReactionLab.Notifications.Mail;

public sealed record MailText(IReadOnlyList<MailTextRun> Runs)
{
    public static MailText Plain(string text) => new([new MailTextRun(text, IsEmphasized: false)]);

    public override string ToString() => string.Concat(Runs.Select(run => run.Text));
}
