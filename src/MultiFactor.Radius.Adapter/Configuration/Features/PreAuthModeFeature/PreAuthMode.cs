namespace MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature
{
    public enum PreAuthMode
    {
        /// <summary>
        /// One-time password
        /// </summary>
        Otp,

        /// <summary>
        /// Mobile app push.
        /// </summary>
        Push,

        /// <summary>
        /// Telegram bot.
        /// </summary>
        Telegram,

        /// <summary>
        /// No mode specified
        /// </summary>
        None
    }
}
