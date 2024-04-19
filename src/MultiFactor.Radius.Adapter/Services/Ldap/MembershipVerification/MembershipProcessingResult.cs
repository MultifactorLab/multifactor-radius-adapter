//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Radius.Adapter/blob/master/LICENSE.md

using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.Ldap.MembershipVerification
{
    /// <summary>
    /// Membership verification result for multiple domains.
    /// </summary>
    public class MembershipProcessingResult
    {
        public MembershipVerificationResult[] Results { get; }

        /// <summary>
        /// Returns only successful results.
        /// </summary>
        public MembershipVerificationResult[] Succeeded => Results
            .Where(x => x.IsSuccess)
            .ToArray();

        public static MembershipProcessingResult Empty => new(Array.Empty<MembershipVerificationResult>());

        public MembershipProcessingResult(IEnumerable<MembershipVerificationResult> results)
        {
            if (results is null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            Results = results.ToArray();
        }
    }
}