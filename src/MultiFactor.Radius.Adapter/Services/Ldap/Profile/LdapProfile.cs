using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Profile;

public class LdapProfile
{
    private readonly string[] _phoneAttrs;
    private string _secondFactorIdentityAttr;

    /// <summary>
    /// Domain base distinguished name (with DC components only).
    /// </summary>
    public LdapIdentity BaseDn { get; }

    /// <summary>
    /// Distinguished name.
    /// </summary>
    public string DistinguishedName => Attributes.DistinguishedName;

    /// <summary>
    /// Distinguished name with escaped special symbols.
    /// </summary>
    public string DistinguishedNameEscaped => EscapeDn(DistinguishedName);

    /// <summary>
    /// User-Principal-Name.
    /// </summary>
    public string Upn => Attributes.GetValue("userprincipalname");

    public string SecondFactorIdentity => string.IsNullOrWhiteSpace(_secondFactorIdentityAttr) ? null : Attributes.GetValue(_secondFactorIdentityAttr);
    public string DisplayName => Attributes.GetValue("displayname");
    public string Email => Attributes.GetValue("mail") ?? Attributes.GetValue("email");

    private string _phone = string.Empty;
    public string Phone
    {
        get
        {
            if (_phone != string.Empty)
            {
                return _phone;
            }

            if (_phoneAttrs.Length == 0)
            {
                _phone = Attributes.GetValue("phone");
                return _phone;
            }

            _phone = _phoneAttrs
                .Select(x => Attributes.GetValue(x))
                .FirstOrDefault(x => x != null) ?? Attributes.GetValue("phone");

            return _phone;
        }
    }

    public ReadOnlyCollection<string> MemberOf => Attributes.GetValues("memberOf");

    public ILdapAttributes Attributes { get; private set; }

    public static LdapProfile Empty => new();

    private LdapProfile()
    {
        BaseDn = null;
        Attributes = new LdapAttributes("dc=localhost");
        _phoneAttrs = Array.Empty<string>();
    }

    public LdapProfile(LdapIdentity baseDn, ILdapAttributes attributes, string[] phoneAttrs, string secondFactorIdentityAttr)
    {
        if (phoneAttrs is null)
        {
            throw new ArgumentNullException(nameof(phoneAttrs));
        }

        BaseDn = baseDn ?? throw new ArgumentNullException(nameof(baseDn));
        Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        _secondFactorIdentityAttr = secondFactorIdentityAttr;
        _phoneAttrs = phoneAttrs.ToArray();
    }

    private static string EscapeDn(string dn)
    {
        var ret = dn
            .Replace("(", @"\28")
            .Replace(")", @"\29");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ret = ret.Replace("\"", "\\\""); // quotes
            ret = ret.Replace("\\,", "\\5C,"); // comma
            ret = ret.Replace("\\=", "\\5C="); // \=
        }

        return ret;
    }

    public LdapProfile SetIdentityAttribute(string identityAttribute)
    {
        _secondFactorIdentityAttr = identityAttribute ?? throw new System.ArgumentNullException(nameof(identityAttribute));
        return this;
    }

    public LdapProfile UpdateAttributes(ILdapAttributes attributes)
    {
        Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        return this;
    }
}
