//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Text;
using System;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;

public class ApiAuthHeaderValue
{
    public string Value { get; }

    public ApiAuthHeaderValue(string usr, string pwd)
    {
        if (string.IsNullOrEmpty(usr))
        {
            throw new ArgumentException($"'{nameof(usr)}' cannot be null or empty.", nameof(usr));
        }

        if (string.IsNullOrEmpty(pwd))
        {
            throw new ArgumentException($"'{nameof(pwd)}' cannot be null or empty.", nameof(pwd));
        }

        Value = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{usr}:{pwd}"));
    }
}