﻿using CCG.Shared.Abstractions.Game.Context.EventSource;
using CCG.Shared.Abstractions.Game.Context.Processors;
using CCG.Shared.Abstractions.Game.Context.Providers;
using CCG.Shared.Common.Utils;
using CCG.Shared.Game.Events.Context.Queue;
using CCG.Shared.Game.Events.Output;

namespace CCG.Shared.Game.Context.Processors
{
    public class ContextEventProcessor : IContextEventProcessor
    {
        private readonly IEventsSource eventsSource;
        private readonly IRuntimeIdProvider idProvider;
        private readonly IRuntimeOrderProvider orderProvider;
        private readonly IRuntimeRandomProvider randomProvider;
        private IDisposable releaseQueueListener;

        public ContextEventProcessor(
            IEventsSource eventsSource,
            IRuntimeIdProvider idProvider,
            IRuntimeOrderProvider orderProvider,
            IRuntimeRandomProvider randomProvider)
        {
            this.eventsSource = eventsSource;
            this.orderProvider = orderProvider;
            this.idProvider = idProvider;
            this.randomProvider = randomProvider;
        }

        public virtual void Start()
        {
            if (releaseQueueListener != null)
                return;

            releaseQueueListener = eventsSource.Subscribe<AfterGameQueueReleasedEvent>(data =>
            {
                var syncRandomEvent = new SyncRuntimeRandom();
                var syncOrderEvent = new SyncRuntimeOrder();
                var syncIdEvent = new SyncRuntimeId();

                syncRandomEvent.Order = orderProvider.Next();
                syncOrderEvent.Order = orderProvider.Next();
                syncIdEvent.Order = orderProvider.Next();

                syncOrderEvent.RuntimeModel = orderProvider.RuntimeModel.DeepCopy();
                syncIdEvent.RuntimeModel = idProvider.RuntimeModel.DeepCopy();
                syncRandomEvent.RuntimeModel = randomProvider.RuntimeModel.DeepCopy();

                data.Queue.Add(syncRandomEvent);
                data.Queue.Add(syncOrderEvent);
                data.Queue.Add(syncIdEvent);
            }, order: int.MinValue);
        }

        public virtual void End()
        {
            releaseQueueListener?.Dispose();
            releaseQueueListener = null;
        }
    }
}