namespace Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum
{
    public enum PreAuthMode
    {
        /// <summary>
        /// No second factor
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Any second factor
        /// </summary>
        Any = 1,

        /// <summary>
        /// One-time password
        /// </summary>
        Otp = 2
    }
}
