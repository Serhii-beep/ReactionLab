using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ReactionLab.Identity.Api.Passwords;

internal sealed class PasswordVerificationGate : IDisposable
{
    private const int DecoySecretLength = 32;

    private readonly SemaphoreSlim _verificationSlots = new(Environment.ProcessorCount);
    private readonly PasswordHasher<PasswordVerificationGate> _decoyHasher;
    private readonly string _decoyHash;

    public PasswordVerificationGate(IOptions<PasswordHasherOptions> options)
    {
        _decoyHasher = new PasswordHasher<PasswordVerificationGate>(options);
        _decoyHash = _decoyHasher.HashPassword(this, RandomNumberGenerator.GetHexString(DecoySecretLength));
    }

    public async Task<TResult> RunAsync<TResult>(Func<TResult> hashingWork, CancellationToken cancellationToken)
    {
        await _verificationSlots.WaitAsync(cancellationToken);

        try
        {
            return hashingWork();
        }
        finally
        {
            _verificationSlots.Release();
        }
    }

    public Task VerifyDecoyAsync(string password, CancellationToken cancellationToken) =>
        RunAsync(() => _decoyHasher.VerifyHashedPassword(this, _decoyHash, password), cancellationToken);

    public void Dispose() => _verificationSlots.Dispose();
}
