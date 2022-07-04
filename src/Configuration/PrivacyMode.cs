﻿//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

namespace MultiFactor.Radius.Adapter.Configuration
{
    /// <summary>
    /// User information disclosure mode
    /// </summary>
    public enum PrivacyMode
    {
        /// <summary>
        /// Include all
        /// </summary>
        None,
        /// <summary>
        /// Disable all but identity
        /// </summary>
        Full
    }
}
