﻿using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;

namespace MultiFactor.Radius.Adapter.Tests.AdapterConfig;

public partial class ConfigurationLoadingTests
{
    [Trait("Category", "Pre Authentication Method")]
    public class PreAuthModeSettingsTests
    {
        [Fact]
        public void DefaultSettings()
        {
            var settings = PreAuthModeSettings.Default;
            Assert.Equal(6, settings.OtpCodeLength);
            Assert.Equal("^[0-9]{6}$", settings.OtpCodeRegex);
        }
        
        [Fact]
        public void CreateSettings_ShoulSuccess()
        {
            var settings = new PreAuthModeSettings(8);
            Assert.Equal(8, settings.OtpCodeLength);
            Assert.Equal("^[0-9]{8}$", settings.OtpCodeRegex);
        }
    }
}