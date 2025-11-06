namespace Multifactor.Radius.Adapter.v2.Services.DataProtection;

public interface IDataProtectionService
{
    string Protect(string secret, string data);

    string Unprotect(string secret, string data);
}