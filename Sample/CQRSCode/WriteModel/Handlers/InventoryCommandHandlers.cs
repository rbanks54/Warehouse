using CQRSCode.WriteModel.Commands;
using CQRSCode.WriteModel.Domain;
using CQRSlite.Commands;
using CQRSlite.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CQRSCode.WriteModel.Handlers
{
    public class InventoryCommandHandlers : ICommandHandler<CreateInventoryItem>,
											ICommandHandler<DeactivateInventoryItem>,
											ICommandHandler<RemoveItemsFromInventory>,
											ICommandHandler<CheckInItemsToInventory>,
											ICommandHandler<RenameInventoryItem>
    {
        private readonly ISession _session;
        private readonly TelemetryClient telemetry;

        public InventoryCommandHandlers(ISession session, TelemetryClient telemetry)
        {
            this.telemetry = telemetry;
            _session = session;
        }

        public void Handle(CreateInventoryItem message)
        {
            var properties = new Dictionary<string, string>()
            {
                {"Name",message.Name},
                {"Item Id", message.Id.ToString()}
            };
            telemetry?.TrackEvent("Received CreateInventoryItem Command", properties);
            var item = new InventoryItem(message.Id, message.Name);
            Thread.Sleep(1500);
            _session.Add(item);
            _session.Commit();
            telemetry?.TrackEvent("Command processing completed");
        }

        public void Handle(DeactivateInventoryItem message)
        {
            telemetry?.TrackEvent("Received DeactivateInventoryItem Command");
            var item = _session.Get<InventoryItem>(message.Id, message.ExpectedVersion);
            item.Deactivate();

            telemetry.TrackTrace("Item deactivated. About to commit",
               SeverityLevel.Information,
               new Dictionary<string, string> { { "item Id", item.Id.ToString() } });

            _session.Commit();
            telemetry?.TrackEvent("Command processing completed");
        }

        public void Handle(RemoveItemsFromInventory message)
        {
            var properties = new Dictionary<string, string>()
            {
                {"Item Id", message.Id.ToString()},
                {"Item Count", message.Count.ToString() }
            };
            telemetry?.TrackEvent("Received RemoveItemsFromInventory Command", properties);
            var item = _session.Get<InventoryItem>(message.Id, message.ExpectedVersion);
            item.Remove(message.Count);
            _session.Commit();
            telemetry?.TrackEvent("Command processing completed");
        }

        public void Handle(CheckInItemsToInventory message)
        {
            telemetry?.TrackEvent("Received CheckInItemsToInventory Command");

            var success = false;
            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var item = _session.Get<InventoryItem>(message.Id, message.ExpectedVersion);
                item.CheckIn(message.Count);
                _session.Commit();
                success = true;
            }
            finally
            {
                timer.Stop();
                telemetry.TrackDependency("persistence", "CheckIn", startTime, timer.Elapsed, success);
            }

            telemetry?.TrackEvent("Command processing completed");
        }

        public void Handle(RenameInventoryItem message)
        {
            telemetry?.TrackEvent("Received RenameInventoryItem Command");
            var item = _session.Get<InventoryItem>(message.Id, message.ExpectedVersion);
            item.ChangeName(message.NewName);
            _session.Commit();
            telemetry?.TrackEvent("Command processing completed");
        }
    }
}
