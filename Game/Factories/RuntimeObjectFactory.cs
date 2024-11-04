﻿using CCG.Shared.Abstractions.Game.Collections;
using CCG.Shared.Abstractions.Game.Context;
using CCG.Shared.Abstractions.Game.Context.Providers;
using CCG.Shared.Abstractions.Game.Factories;
using CCG.Shared.Abstractions.Game.Runtime;
using CCG.Shared.Abstractions.Game.Runtime.Models;
using CCG.Shared.Game.Config;
using CCG.Shared.Game.Enums;
using CCG.Shared.Game.Runtime.Cards;
using CCG.Shared.Game.Runtime.Models;

namespace CCG.Shared.Game.Factories
{
    public class RuntimeObjectFactory : IRuntimeObjectFactory
    {
        private readonly IDatabase database;
        private readonly IObjectsCollection objectsCollection;
        private readonly IRuntimeIdProvider runtimeIdProvider;
        private readonly IRuntimeStatFactory runtimeStatFactory;
        private readonly IRuntimeEffectFactory runtimeEffectFactory;
        private readonly IContextFactory contextFactory;

        public RuntimeObjectFactory(
            IDatabase database,
            IObjectsCollection objectsCollection,
            IRuntimeIdProvider runtimeIdProvider,
            IRuntimeStatFactory runtimeStatFactory,
            IRuntimeEffectFactory runtimeEffectFactory,
            IContextFactory contextFactory)
        {
            this.database = database;
            this.objectsCollection = objectsCollection;
            this.runtimeIdProvider = runtimeIdProvider;
            this.runtimeStatFactory = runtimeStatFactory;
            this.runtimeEffectFactory = runtimeEffectFactory;
            this.contextFactory = contextFactory;
        }

        public IRuntimeObjectModel CreateModel(int? runtimeId, string ownerId, string dataId, bool notify = true)
        {
            if (!database.Objects.TryGet(dataId, out var data))
                throw new NullReferenceException($"{nameof(ObjectConfig)} with id {dataId}, not found in {nameof(IConfigCollection<ObjectConfig>)}");

            runtimeId ??= runtimeIdProvider.Next();
            return data.Type switch
            {
                ObjectType.Creature or ObjectType.Spell => new RuntimeCardModel
                {
                    ConfigId = data.Id,
                    Id = runtimeId.Value,
                    OwnerId = ownerId,
                    Stats = data.Stats.Select(id => runtimeStatFactory.CreateModel(runtimeId.Value, ownerId, id, notify)).ToList(),
                },
                _ => throw new NotImplementedException($"Unknown {nameof(ObjectType)}: {data.Type}")
            };
        }
        
        public IRuntimeObject Create(IRuntimeObjectModel runtimeModel, bool notify = true)
        {
            if (objectsCollection.TryGet(runtimeModel.Id, out var runtimeObject))
                return runtimeObject.Sync(runtimeModel, notify);
            
            if (!database.Objects.TryGet(runtimeModel.ConfigId, out var data))
                throw new NullReferenceException($"{nameof(ObjectConfig)} with id {runtimeModel.ConfigId}, not found in {nameof(IConfigCollection<ObjectConfig>)}");
            
            var eventSource = contextFactory.CreateEventsSource();
            var eventPublisher = contextFactory.CreateEventPublisher(eventSource);
            var statsCollection = contextFactory.CreateStatsCollection(eventPublisher);
            var effectsCollection = contextFactory.CreateEffectsCollection(eventPublisher);
            runtimeObject = data.Type switch
            {
                ObjectType.Creature => new RuntimeCardCreature().Init(data, statsCollection, effectsCollection, eventPublisher, eventSource),
                ObjectType.Spell => new RuntimeCardSpell().Init(data, statsCollection, effectsCollection, eventPublisher, eventSource),
                _ => throw new NotImplementedException($"Unknown {nameof(ObjectType)}: {data.Type}")
            };
            
            objectsCollection.Add(runtimeObject, notify);
            
            foreach (var runtimeStatModel in runtimeModel.Stats)
                runtimeStatFactory.Create(runtimeStatModel, false);
            
            foreach (var runtimeEffectModel in runtimeModel.Applied)
                runtimeEffectFactory.Create(runtimeEffectModel, false);
            
            return runtimeObject;
        }
    }
}