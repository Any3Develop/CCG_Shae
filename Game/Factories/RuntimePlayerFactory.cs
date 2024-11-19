﻿using CCG.Shared.Abstractions.Game.Collections;
using CCG.Shared.Abstractions.Game.Context.Providers;
using CCG.Shared.Abstractions.Game.Factories;
using CCG.Shared.Abstractions.Game.Runtime;
using CCG.Shared.Abstractions.Game.Runtime.Models;
using CCG.Shared.Game.Config;
using CCG.Shared.Game.Runtime;
using CCG.Shared.Game.Runtime.Models;

namespace CCG.Shared.Game.Factories
{
    public class RuntimePlayerFactory : IRuntimePlayerFactory
    {
        private readonly IRuntimeStatFactory runtimeStatFactory;
        private readonly ISharedConfig sharedConfig;
        private readonly IPlayersCollection playersCollection;
        private readonly IRuntimeIdProvider runtimeIdProvider;
        private readonly IContextFactory contextFactory;

        public RuntimePlayerFactory(
            ISharedConfig sharedConfig,
            IPlayersCollection playersCollection,
            IRuntimeIdProvider runtimeIdProvider,
            IRuntimeStatFactory runtimeStatFactory,
            IContextFactory contextFactory)
        {
            this.sharedConfig = sharedConfig;
            this.playersCollection = playersCollection;
            this.runtimeIdProvider = runtimeIdProvider;
            this.runtimeStatFactory = runtimeStatFactory;
            this.contextFactory = contextFactory;
        }

        public IRuntimePlayerModel CreateModel(string ownerId, int index)
        {
            var dataId = $"default-player-{index}";
            var playerConfig = sharedConfig.Players.FirstOrDefault(x => x.Id == dataId);
            if (playerConfig == null)
                throw new NullReferenceException($"{nameof(PlayerConfig)} with id {dataId}, not found in {nameof(ISharedConfig)}");

            var runtimeId = runtimeIdProvider.Next();
            return new RuntimePlayerModel
            {
                Id = runtimeId,
                OwnerId = ownerId,
                ConfigId = dataId,
                Stats = playerConfig.Stats
                    .Select(statId => runtimeStatFactory.CreateModel(runtimeId, ownerId, statId))
                    .ToList()
            };
        }

        public IRuntimePlayer Create(IRuntimePlayerModel runtimeModel, bool notify = false)
        {
            var runtimePlayer = CreateInternal(runtimeModel);
            InitIntrnal(runtimePlayer, false);
            
            if (notify)
                playersCollection.AddNotify(runtimePlayer);
            
            return runtimePlayer;
        }

        public void Restore(IEnumerable<IRuntimePlayerModel> runtimeModels)
        {
            foreach (var runtimePlayer in runtimeModels.Reverse().Select(CreateInternal))
                InitIntrnal(runtimePlayer, true);
        }

        private IRuntimePlayer CreateInternal(IRuntimePlayerModel runtimeModel)
        {
            if (playersCollection.Contains(runtimeModel.OwnerId))
                throw new InvalidOperationException($"Unable create a player twice : {runtimeModel.OwnerId}");
            
            var playerConfig = sharedConfig.Players.FirstOrDefault(x => x.Id == runtimeModel.ConfigId);
            if (playerConfig == null)
                throw new NullReferenceException($"{nameof(PlayerConfig)} with id {runtimeModel.ConfigId}, not found in {nameof(ISharedConfig)}");
            
            var eventSource = contextFactory.CreateEventsSource();
            var eventPublisher = contextFactory.CreateEventPublisher(eventSource);
            var statsCollection = contextFactory.CreateStatsCollection(eventPublisher);
            var runtimePlayer = new RuntimePlayer(playerConfig, runtimeModel, statsCollection, eventPublisher, eventSource);
            
            playersCollection.Add(runtimePlayer, false);
            return runtimePlayer;
        }

        private void InitIntrnal(IRuntimePlayer runtimePlayer, bool reversed)
        {
            var statModels = reversed
                ? runtimePlayer.RuntimeModel.Stats.AsEnumerable().Reverse()
                : runtimePlayer.RuntimeModel.Stats;
            
            foreach (var runtimeModel in statModels)
                runtimeStatFactory.Create(runtimeModel, false);
        }
    }
}