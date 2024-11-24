﻿namespace CCG.Shared.Abstractions.Game.Runtime.Models
{
    public interface IRuntimePlayerModel : IRuntimeBaseModel
    {
        string Name { get; }
        bool Ready { get; set; }
        bool IsFirst { get; set; }
        List<IRuntimeStatModel> Stats { get; }
    }
}