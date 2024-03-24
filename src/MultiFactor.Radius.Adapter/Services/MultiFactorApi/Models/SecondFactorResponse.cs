//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Radius;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi.Models;

public record SecondFactorResponse(PacketCode Code, string State = null, string ReplyMessage = null);
