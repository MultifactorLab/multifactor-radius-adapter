//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using System.Collections.Generic;

namespace MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification
{
    public interface IMembershipProcessingResult
    {
        IReadOnlyList<MembershipVerificationResult> Results { get; }
        IReadOnlyList<MembershipVerificationResult> Succeeded { get; }
    }
}