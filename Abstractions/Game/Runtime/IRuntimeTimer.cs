﻿using CCG.Shared.Abstractions.Game.Runtime.Models;
using CCG.Shared.Game.Config;
using CCG.Shared.Game.Enums;

namespace CCG.Shared.Abstractions.Game.Runtime
{
    public interface IRuntimeTimer : IDisposable
    {
        TimerConfig Config { get; }
        IRuntimeTimerModel RuntimeModel { get; }
        
        IRuntimeTimer Sync(IRuntimeTimerModel runtimeModel);
        void SetState(TimerState value, bool notify = true);
        void SetTurnOwner(string value, bool notify = true);
        void PassTurn(bool notify = true);
    }
}