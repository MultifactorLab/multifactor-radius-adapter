namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.SharedServices.ChallengeProcessor.Models;

public sealed class PasswordChangeValue
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Domain { get; set; }
    public string CurrentPasswordEncryptedData { get; set; }
    public string? NewPasswordEncryptedData { get; set; }
}