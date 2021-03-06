namespace Dns.Domain.Services.Manual.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Be.Vlaanderen.Basisregisters.EventHandling;
    using Dns.Domain.Events;
    using Manual;
    using Newtonsoft.Json;

    [EventName("ManualWasAdded")]
    [EventDescription("The manual service was added.")]
    public class ManualWasAdded
    {
        [JsonIgnore]
        public string DomainName { get; }
        public string SecondLevelDomain { get; }
        public string TopLevelDomain { get; }

        public Guid ServiceId { get; }
        public string ServiceType { get; }
        public string ServiceLabel { get; }

        public RecordData[] Records { get; }

        public ManualWasAdded(
            DomainName domainName,
            ServiceId serviceId,
            ManualLabel label,
            RecordSet recordSet)
        {
            DomainName = domainName;
            SecondLevelDomain = domainName.SecondLevelDomain;
            TopLevelDomain = domainName.TopLevelDomain.Value;

            ServiceId = serviceId;

            Records = recordSet
                .Select(r => new RecordData(r))
                .ToArray();

            var service = new ManualService(serviceId, label, recordSet);
            ServiceType = service.Type.Value;
            ServiceLabel = service.Label;
        }

        [JsonConstructor]
        private ManualWasAdded(
            [JsonProperty("secondLevelDomain")] string secondLevelDomain,
            [JsonProperty("topLevelDomain")] string topLevelDomain,
            [JsonProperty("serviceId")] Guid serviceId,
            [JsonProperty("serviceLabel")] string serviceLabel,
            [JsonProperty("records")] IReadOnlyCollection<RecordData> records)
            : this(
                new DomainName(
                    new SecondLevelDomain(secondLevelDomain),
                    Dns.TopLevelDomain.FromValue(topLevelDomain)),
                new ServiceId(serviceId),
                new ManualLabel(serviceLabel),
                new RecordSet(records.Select(r => r.ToRecord()).ToList())) { }
    }
}
