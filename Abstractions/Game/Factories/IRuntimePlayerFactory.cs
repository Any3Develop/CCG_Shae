﻿using CCG.Shared.Abstractions.Game.Runtime;
using CCG.Shared.Abstractions.Game.Runtime.Models;

namespace CCG.Shared.Abstractions.Game.Factories
{
    public interface IRuntimePlayerFactory
    {
        IRuntimePlayerModel CreateModel(string ownerId, int index);
        IRuntimePlayer Create(IRuntimePlayerModel runtimeModel, bool notify = false);
        void Restore(IEnumerable<IRuntimePlayerModel> runtimeModels);
    }
}