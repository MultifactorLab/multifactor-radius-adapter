namespace MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature
{
    public enum PreAuthMode
    {
        /// <summary>
        /// No mode specified
        /// </summary>
        None = 0,

        /// <summary>
        /// One-time password
        /// </summary>
        Otp = 1,

        /// <summary>
        /// Mobile app push.
        /// </summary>
        Push = 2,

        /// <summary>
        /// Telegram bot.
        /// </summary>
        Telegram = 4
    }
}
