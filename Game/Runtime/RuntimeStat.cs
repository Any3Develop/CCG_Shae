﻿using CCG.Shared.Abstractions.Game.Context;
using CCG.Shared.Abstractions.Game.Context.EventSource;
using CCG.Shared.Abstractions.Game.Runtime;
using CCG.Shared.Abstractions.Game.Runtime.Models;
using CCG.Shared.Game.Config;
using CCG.Shared.Game.Events.Context.Stats;

namespace CCG.Shared.Game.Runtime
{
    public class RuntimeStat : IRuntimeStat
    {
        public StatConfig Config { get; private set; }
        public IRuntimeStatModel RuntimeModel { get; private set; }
        public IEventPublisher EventPublisher { get; private set; }
        public IEventsSource EventsSource { get; private set; }


        public IRuntimeStat Init(
            StatConfig config,
            IEventPublisher eventPublisher,
            IEventsSource eventsSource)
        {
            Config = config;
            EventPublisher = eventPublisher;
            EventsSource = eventsSource;
            return this;
        }

        public virtual void Dispose()
        {
            Config = null;
            RuntimeModel = null;
            EventsSource = null;
        }

        public IRuntimeStat Sync(IRuntimeStatModel runtimeModel)
        {
            RuntimeModel = runtimeModel;
            return this;
        }

        public virtual void SetValue(int value, bool notify = true)
        {
            OnBeforeChanged(notify);
            RuntimeModel.Value = Math.Min(value, RuntimeModel.Max);
            OnAfterChanged(notify);
        }

        public virtual void SetMax(int value, bool notify = true)
        {
            OnBeforeChanged(notify);
            RuntimeModel.Max = value;
            SetValue(RuntimeModel.Value, false);
            OnAfterChanged(notify);
        }

        public virtual void Reset(bool notify = true)
        {
            OnBeforeChanged(notify);
            RuntimeModel.Max = Config.Max;
            RuntimeModel.Value = Config.Value;
            OnAfterChanged(notify);
        }

        #region Callbacks

        protected virtual void OnBeforeChanged(bool notify = true)
        {
            if (notify)
                EventPublisher.Publish(new BeforeStatChangeEvent(this));
        }

        protected virtual void OnAfterChanged(bool notify = true)
        {
            if (notify)
                EventPublisher.Publish(new AfterStatChangedEvent(this));
        }

        #endregion

        #region IRuntimeObjectBase

        IRuntimeModelBase IRuntimeObjectBase.RuntimeModel => RuntimeModel;

        IConfig IRuntimeObjectBase.Config => Config;

        #endregion
    }
}