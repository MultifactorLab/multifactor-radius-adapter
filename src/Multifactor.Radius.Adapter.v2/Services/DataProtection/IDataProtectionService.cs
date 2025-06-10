namespace Multifactor.Radius.Adapter.v2.Services.DataProtection;

public interface IDataProtectionService
{
    string Protect(string data);

    string Unprotect(string data);
}