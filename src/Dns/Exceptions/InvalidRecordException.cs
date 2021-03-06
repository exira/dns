namespace Dns.Exceptions
{
    using System;

    public class InvalidRecordException : DnsException
    {
        public InvalidRecordException(string message) : base(message) { }

        public InvalidRecordException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidRecordLabelException : InvalidRecordException
    {
        public InvalidRecordLabelException(string message) : base(message) { }

        public InvalidRecordLabelException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidRecordValueException : InvalidRecordException
    {
        public InvalidRecordValueException(string message) : base(message) { }

        public InvalidRecordValueException(string message, Exception inner) : base(message, inner) { }
    }

    public class InvalidTimeToLiveException : InvalidRecordException
    {
        public InvalidTimeToLiveException(string message) : base(message) { }
    }

    public class EmptyRecordLabelException : InvalidRecordLabelException
    {
        public EmptyRecordLabelException() : base("Label of a record cannot be empty.") { }
    }

    public class RecordLabelTooLongException : InvalidRecordLabelException
    {
        public RecordLabelTooLongException() : base($"Label of a record cannot be longer than {RecordLabel.MaxLength} characters.") { }
    }

    public class RecordLabelContainsInvalidCharactersException : InvalidRecordLabelException
    {
        public RecordLabelContainsInvalidCharactersException() : base("Label of a record contains invalid characters.") { }
    }

    public class RecordLabelCannotStartWithDashException : InvalidRecordLabelException
    {
        public RecordLabelCannotStartWithDashException() : base("Label of a record cannot start with a dash.") { }
    }

    public class RecordLabelCannotEndWithDashException : InvalidRecordLabelException
    {
        public RecordLabelCannotEndWithDashException() : base("Label of a record cannot end with a dash.") { }
    }

    public class RecordLabelCannotBeAllDigitsException : InvalidRecordLabelException
    {
        public RecordLabelCannotBeAllDigitsException() : base("Label of a record cannot consist out of digits only.") { }
    }

    public class RecordValueTooLongException : InvalidRecordLabelException
    {
        public RecordValueTooLongException() : base($"Value of a record cannot be longer than {RecordValue.MaxLength} characters.") { }
    }

    public class EmptyRecordValueException : InvalidRecordValueException
    {
        public EmptyRecordValueException() : base("Value of a record cannot be empty.") { }
    }

    public class RecordValueARecordMustBeValidIpException : InvalidRecordValueException
    {
        public RecordValueARecordMustBeValidIpException() : base("Value of an A record must be a dotted-quad IP address.") { }
    }

    public class RecordValueCNameRecordInvalidHostnameException : InvalidRecordValueException
    {
        public RecordValueCNameRecordInvalidHostnameException() : base("Value of a CNAME record must be a label, or a hostname ending with a dot.") { }
    }

    public class RecordValueCNameRecordInvalidLabelException : InvalidRecordValueException
    {
        public RecordValueCNameRecordInvalidLabelException() : base("Value of a CNAME record must be a label, or a hostname ending with a dot.") { }
    }

    public class RecordValueMxRecordMustHavePriorityAndHostnameException : InvalidRecordValueException
    {
        public RecordValueMxRecordMustHavePriorityAndHostnameException() : base("Value of an MX record must be a 16-bit integer priority field, and a legal hostname or dns label.") { }
    }

    public class RecordValueMxRecordMustHaveIntegerPriorityException : InvalidRecordValueException
    {
        public RecordValueMxRecordMustHaveIntegerPriorityException() : base("Value of an MX record must be a 16-bit integer priority field, and a legal hostname or dns label.") { }
    }

    public class RecordValueMxRecordMustHaveHostnameException : InvalidRecordValueException
    {
        public RecordValueMxRecordMustHaveHostnameException() : base("Value of an MX record must be a 16-bit integer priority field, and a legal hostname or dns label.") { }
    }

    public class RecordValueMxRecordInvalidHostnameException : InvalidRecordValueException
    {
        public RecordValueMxRecordInvalidHostnameException() : base("Value of an MX record must be a 16-bit integer priority field, and a legal hostname or dns label.") { }
    }

    public class RecordValueMxRecordInvalidLabelException : InvalidRecordValueException
    {
        public RecordValueMxRecordInvalidLabelException() : base("Value of an MX record must be a 16-bit integer priority field, and a legal hostname or dns label.") { }
    }

    public class RecordValueNsRecordInvalidHostnameException : InvalidRecordValueException
    {
        public RecordValueNsRecordInvalidHostnameException() : base("Value of an NS record must be a legal hostname or dns label.") { }
    }

    public class RecordValueNsRecordInvalidLabelException : InvalidRecordValueException
    {
        public RecordValueNsRecordInvalidLabelException() : base("Value of an NS record must be a legal hostname or dns label.") { }
    }

    public class NegativeTimeToLiveException : InvalidTimeToLiveException
    {
        public NegativeTimeToLiveException() : base("Time to live cannot be negative.") { }
    }
}
