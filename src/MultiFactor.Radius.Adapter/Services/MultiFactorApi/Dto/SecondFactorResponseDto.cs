//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Core.Radius;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;

public class SecondFactorResponseDto
{
    public PacketCode Code { get; }
    public string ChallengeState { get; }
    public string ReplyMessage { get; }

    public SecondFactorResponseDto(PacketCode code, string state = null, string replyMessage = null)
    {
        Code = code;
        ChallengeState = state;
        ReplyMessage = replyMessage;
    }
}
