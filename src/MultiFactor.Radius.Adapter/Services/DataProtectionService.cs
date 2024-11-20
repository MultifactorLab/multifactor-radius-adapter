using System;
using Microsoft.AspNetCore.DataProtection;

namespace MultiFactor.Radius.Adapter.Services;

public class DataProtectionService
{
    private readonly IDataProtectionProvider _protectionProvider;
    public DataProtectionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protectionProvider = dataProtectionProvider;
    }
    
    public string Protect(string data, string protectionProviderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));

        var protector = _protectionProvider.CreateProtector(protectionProviderName);
        var encrypted =  protector.Protect(data);
        
        return encrypted;
    }

    public string Unprotect(string data, string protectionProviderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));
        
        var protector = _protectionProvider.CreateProtector(protectionProviderName);
        var decrypted = protector.Unprotect(data);
        
        return decrypted;
    }
}