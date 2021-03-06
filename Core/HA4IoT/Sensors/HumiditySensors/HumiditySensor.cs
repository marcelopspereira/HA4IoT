﻿using System;
using HA4IoT.Components;
using HA4IoT.Components.Commands;
using HA4IoT.Contracts.Components;
using HA4IoT.Contracts.Components.Adapters;
using HA4IoT.Contracts.Components.Commands;
using HA4IoT.Contracts.Components.Features;
using HA4IoT.Contracts.Components.States;
using HA4IoT.Contracts.Sensors;
using HA4IoT.Contracts.Settings;

namespace HA4IoT.Sensors.HumiditySensors
{
    public class HumiditySensor : ComponentBase, IHumiditySensor
    {
        private readonly object _syncRoot = new object();

        private readonly CommandExecutor _commandExecutor = new CommandExecutor();
        private readonly INumericSensorAdapter _adapter;
        private float? _value;

        public HumiditySensor(string id, INumericSensorAdapter adapter, ISettingsService settingsService)
            : base(id)
        {
            if (settingsService == null) throw new ArgumentNullException(nameof(settingsService));

            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            settingsService.CreateSettingsMonitor<SingleValueSensorSettings>(this, s => Settings = s.NewSettings);

            _adapter.ValueChanged += (s, e) => Update(e.Value);

            _commandExecutor.Register<ResetCommand>(c => _adapter.Refresh());
        }

        public SingleValueSensorSettings Settings { get; private set; }

        public override IComponentFeatureCollection GetFeatures()
        {
            return new ComponentFeatureCollection()
                .With(new HumidityMeasurementFeature());
        }

        public override IComponentFeatureStateCollection GetState()
        {
            return new ComponentFeatureStateCollection()
                .With(new HumidityState(_value));
        }

        public override void ExecuteCommand(ICommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            lock (_syncRoot)
            {
                _commandExecutor.Execute(command);
            }
        }

        private void Update(float? newValue)
        {
            if (!GetDifferenceIsLargeEnough(newValue))
            {
                return;
            }

            var oldState = GetState();
            _value = newValue;
            OnStateChanged(oldState);
        }

        private bool GetDifferenceIsLargeEnough(float? newValue)
        {
            if (_value.HasValue != newValue.HasValue)
            {
                return true;
            }

            if (!_value.HasValue)
            {
                return false;
            }

            if (!newValue.HasValue)
            {
                return false;
            }

            return Math.Abs(_value.Value - newValue.Value) >= Settings.MinDelta;
        }
    }
}