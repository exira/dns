namespace Dns.Api.Domain
{
    using System;
    using Be.Vlaanderen.Basisregisters.Api.Localization;
    using FluentValidation;
    using Infrastructure;
    using Microsoft.Extensions.Localization;

    public class DomainValidatorsResources
    {
        public string RequiredMessage => "{PropertyName} is required.";
        public string MaxLengthMessage => "{PropertyName} cannot be longer than {MaxLength} characters.";
        public string ValidHostNameMessage => "{PropertyName} must be a valid hostname.";
    }

    public static class DomainValidators
    {
        private static readonly IStringLocalizer<DomainValidatorsResources> Localizer =
            GlobalStringLocalizer.Instance.GetLocalizer<DomainValidatorsResources>();

        public static IRuleBuilderOptions<T, Guid?> Required<T>(this IRuleBuilder<T, Guid?> ruleBuilder)
            => ruleBuilder
                .NotEmpty().WithMessage(Localizer.GetString(x => x.RequiredMessage))
                .NotEqual(Guid.Empty).WithMessage(Localizer.GetString(x => x.RequiredMessage));

        public static IRuleBuilderOptions<T, string> Required<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder.NotEmpty().WithMessage(Localizer.GetString(x => x.RequiredMessage));

        public static IRuleBuilderOptions<T, string> MaxLength<T>(this IRuleBuilder<T, string> ruleBuilder, int length)
            => ruleBuilder.Length(0, length).WithMessage(Localizer.GetString(x => x.MaxLengthMessage));

        public static IRuleBuilderOptions<T, string> ValidHostName<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder
                .Must(property => Uri.CheckHostName(property) == UriHostNameType.Dns)
                .WithMessage(Localizer.GetString(x => x.ValidHostNameMessage));

        public static IRuleBuilderOptions<T, string> ValidTopLevelDomain<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder.ValidEnumeration<T, TopLevelDomain, InvalidTopLevelDomainException>();
    }
}
