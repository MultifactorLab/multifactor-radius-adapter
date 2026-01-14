namespace Multifactor.Radius.Adapter.v2.Application.Features.AccessChallenge.Models;

public class PasswordChangeCache
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Domain { get; set; }
    public string CurrentPasswordEncryptedData { get; set; }
    public string NewPasswordEncryptedData { get; set; }
}