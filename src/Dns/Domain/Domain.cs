namespace Dns.Domain
{
    using System;
    using System.Linq;
    using Be.Vlaanderen.Basisregisters.AggregateSource;
    using Events;
    using Exceptions;
    using Services.GoogleSuite;
    using Services.GoogleSuite.Events;
    using Services.GoogleSuite.Exceptions;
    using Services.Manual;
    using Services.Manual.Events;

    public partial class Domain : AggregateRootEntity
    {
        public static readonly Func<Domain> Factory = () => new Domain();

        public static Domain Register(DomainName domainName)
        {
            var domain = Factory();
            domain.ApplyChange(new DomainWasCreated(domainName));
            return domain;
        }

        public void AddManual(ServiceId serviceId, ManualLabel label, RecordSet records)
        {
            CheckIfServiceAlreadyExists(serviceId);
            ApplyChange(new ManualWasAdded(_name, serviceId, label, records));
            UpdateRecordSet();
        }

        public void AddGoogleSuite(ServiceId serviceId, GoogleVerificationToken verificationToken)
        {
            CheckIfServiceAlreadyExists(serviceId);

            if (_services.Any(x => x.Value.Type.Value == ServiceType.googlesuite.Value))
                throw new GoogleSuiteServiceAlreadyExistsException();

            ApplyChange(new GoogleSuiteWasAdded(_name, serviceId, verificationToken));
            UpdateRecordSet();
        }

        public void RemoveService(ServiceId serviceId)
        {
            if (!_services.ContainsKey(serviceId))
                return;

            ApplyChange(new ServiceWasRemoved(_name, serviceId));
            UpdateRecordSet();
        }

        private void CheckIfServiceAlreadyExists(ServiceId serviceId)
        {
            if (_services.ContainsKey(serviceId))
                throw new ServiceAlreadyExistsException(serviceId);
        }

        private void UpdateRecordSet()
        {
            ApplyChange(
                new RecordSetWasUpdated(
                    _name,
                    _services.Values.Aggregate(
                        new RecordSet(),
                        (r, service) => r.AddRecords(service.GetRecords()),
                        r => r)));
        }
    }
}
