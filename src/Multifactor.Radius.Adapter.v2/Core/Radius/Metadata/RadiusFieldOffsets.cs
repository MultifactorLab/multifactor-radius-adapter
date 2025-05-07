namespace Multifactor.Radius.Adapter.v2.Core.Radius.Metadata
{
    internal static class RadiusFieldOffsets
    {
        public const int CodeFieldPosition = 0;
        public const int IdentifierFieldPosition = 1;

        public const int LengthFieldPosition = 2;
        public const int LengthFieldLength = 2;

        public const int AuthenticatorFieldPosition = 4;
        public const int AuthenticatorFieldLength = 16;

        public const int AttributesFieldPosition = 20;
    }
}
