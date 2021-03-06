namespace Dns.Api.Domain.Exceptions
{
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Be.Vlaanderen.Basisregisters.BasicApiProblem;
    using Infrastructure.Exceptions;
    using Microsoft.AspNetCore.Http;

    public class InvalidTopLevelDomainExceptionHandling : DefaultExceptionHandler<InvalidTopLevelDomainException>
    {
        protected override ProblemDetails GetApiProblemFor(InvalidTopLevelDomainException exception)
            => new ProblemDetails
            {
                HttpStatus = StatusCodes.Status400BadRequest,
                Title = Constants.DefaultTitle,
                Detail = $"'{exception.Value}' is not valid for {exception.Type}.",
                ProblemInstanceUri = ProblemDetails.GetProblemNumber(),
                ProblemTypeUri = ProblemDetails.GetTypeUriFor(exception)
            };
    }
}
