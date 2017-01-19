using System;
using CQRSCode.ReadModel.Dtos;
using CQRSCode.ReadModel.Events;
using CQRSCode.ReadModel.Infrastructure;
using CQRSlite.Events;
using Microsoft.ApplicationInsights;
using System.Collections.Generic;

namespace CQRSCode.ReadModel.Handlers
{
    public class InventoryItemDetailView : IEventHandler<InventoryItemCreated>,
											IEventHandler<InventoryItemDeactivated>,
											IEventHandler<InventoryItemRenamed>,
											IEventHandler<ItemsRemovedFromInventory>,
											IEventHandler<ItemsCheckedInToInventory>
    {
        private TelemetryClient telemetry;

        public InventoryItemDetailView(TelemetryClient telemetry)
        {
            this.telemetry = telemetry;
        }

        public void Handle(InventoryItemCreated message)
        {
            var properties = new Dictionary<string, string>()
            {
                {"Name",message.Name},
                {"Id", message.Id.ToString() },
                {"Timestamp", message.TimeStamp.ToString() }
            };
            var metrics = new Dictionary<string, double>() {
                { "Version", message.Version }
            };
            telemetry?.TrackEvent("Receieved InventoryItemCreated", properties, metrics);

            InMemoryDatabase.Details.Add(message.Id, new InventoryItemDetailsDto(message.Id, message.Name, 0, message.Version));
        }

        public void Handle(InventoryItemRenamed message)
        {
            telemetry?.TrackEvent("Receieved InventoryItemRenamed");
            InventoryItemDetailsDto d = GetDetailsItem(message.Id);
            d.Name = message.NewName;
            d.Version = message.Version;
        }

        private InventoryItemDetailsDto GetDetailsItem(Guid id)
        {
            InventoryItemDetailsDto dto;
            if(!InMemoryDatabase.Details.TryGetValue(id, out dto))
            {
                throw new InvalidOperationException("did not find the original inventory this shouldnt happen");
            }
            return dto;
        }

        public void Handle(ItemsRemovedFromInventory message)
        {
            telemetry?.TrackEvent("Receieved ItemsRemovedFromInventory");
            var dto = GetDetailsItem(message.Id);
            dto.CurrentCount -= message.Count;
            dto.Version = message.Version;
        }

        public void Handle(ItemsCheckedInToInventory message)
        {
            telemetry?.TrackEvent("Receieved ItemsCheckedInToInventory");
            var dto = GetDetailsItem(message.Id);
            dto.CurrentCount += message.Count;
            dto.Version = message.Version;
        }

        public void Handle(InventoryItemDeactivated message)
        {
            telemetry?.TrackEvent("Receieved InventoryItemDeactivated");
            InMemoryDatabase.Details.Remove(message.Id);
        }
    }
}
