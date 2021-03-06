namespace Dns.Api.Domain
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Api;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Be.Vlaanderen.Basisregisters.Api.Search;
    using Be.Vlaanderen.Basisregisters.Api.Search.Filtering;
    using Be.Vlaanderen.Basisregisters.Api.Search.Pagination;
    using Be.Vlaanderen.Basisregisters.Api.Search.Sorting;
    using Be.Vlaanderen.Basisregisters.CommandHandling;
    using Infrastructure;
    using Infrastructure.LastObservedPosition;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json.Converters;
    using Projections.Api;
    using Projections.Api.DomainDetail;
    using Projections.Api.ServiceDetail;
    using Query;
    using Requests;
    using Responses;
    using Swashbuckle.AspNetCore.Filters;
    using ProblemDetails = Be.Vlaanderen.Basisregisters.BasicApiProblem.ProblemDetails;

    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [ApiRoute("domains")]
    [ApiExplorerSettings(GroupName = "Domain")]
    public partial class DomainController : DnsController
    {
        /// <summary>
        /// Register a domain.
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="commandId">Optional unique identifier for the request.</param>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <response code="202">If the request has been accepted.</response>
        /// <response code="400">If the request contains invalid data.</response>
        /// <response code="500">If an internal error has occurred.</response>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(LastObservedPositionResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerRequestExample(typeof(CreateDomainRequest), typeof(CreateDomainRequestExample))]
        [SwaggerResponseExample(StatusCodes.Status202Accepted, typeof(LastObservedPositionResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        public async Task<IActionResult> CreateDomain(
            [FromServices] ICommandHandlerResolver bus,
            [FromCommandId] Guid commandId,
            [FromBody] CreateDomainRequest request,
            CancellationToken cancellationToken = default)
        {
            // TODO: Get this validator from DI
            await new CreateDomainRequestValidator()
                .ValidateAndThrowAsync(request, cancellationToken: cancellationToken);

            var command = CreateDomainRequestMapping.Map(request);

            // TODO: Sending null for top level domain should give a decent error, not 500
            // TODO: Apikey description in documentation should be translatable
            // TODO: Add bad format response code if it is not json

            return Accepted(
                $"/v1/domains/{command.DomainName}",
                new LastObservedPositionResponse(
                    await bus.Dispatch(
                        commandId,
                        command,
                        GetMetadata(),
                        cancellationToken)));
        }

        /// <summary>
        /// List domains.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">If the domain is found.</response>
        /// <response code="404">If the domain does not exist.</response>
        /// <response code="500">If an internal error has occurred.</response>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(DomainListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DomainListResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        public async Task<IActionResult> ListDomains(
            [FromServices] ApiProjectionsContext context,
            CancellationToken cancellationToken = default)
        {
            // TODO: Add support for eventual consistency

            var filtering = Request.ExtractFilteringRequest<DomainListFilter>();
            var sorting = Request.ExtractSortingRequest();
            var pagination = Request.ExtractPaginationRequest();

            var pagedDomains = new DomainListQuery(context)
                .Fetch(filtering, sorting, pagination);

            Response.AddPagedQueryResultHeaders(pagedDomains);

            return Ok(
                new DomainListResponse
                {
                    Domains = await pagedDomains
                        .Items
                        .Select(x => new DomainListItemResponse(x))
                        .ToListAsync(cancellationToken)
                });
        }

        /// <summary>
        /// List details of a domain.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="secondLevelDomain">Second level domain of the domain to list details for.</param>
        /// <param name="topLevelDomain">Top level domain of the domain to list details for.</param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">If the domain is found.</response>
        /// <response code="400">If the request contains invalid data.</response>
        /// <response code="404">If the domain does not exist.</response>
        /// <response code="500">If an internal error has occurred.</response>
        /// <returns></returns>
        [HttpGet("{secondLevelDomain}.{topLevelDomain}")]
        [ProducesResponseType(typeof(DomainDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DomainDetailResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(DomainNotFoundResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        public async Task<IActionResult> DetailDomain(
            [FromServices] ApiProjectionsContext context,
            [FromRoute] string secondLevelDomain,
            [FromRoute] string topLevelDomain,
            CancellationToken cancellationToken = default)
        {
            var request = new DetailDomainRequest
            {
                SecondLevelDomain = secondLevelDomain,
                TopLevelDomain = topLevelDomain,
            };

            await new DetailDomainRequestValidator()
                .ValidateAndThrowAsync(request, cancellationToken: cancellationToken);

            return Ok(
                new DomainDetailResponse(
                    await FindDomainAsync(
                        context,
                        secondLevelDomain,
                        topLevelDomain,
                        cancellationToken)));
        }

        /// <summary>
        /// List services of a domain.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="secondLevelDomain">Second level domain of the domain to list services for.</param>
        /// <param name="topLevelDomain">Top level domain of the domain to list services for.</param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">If the domain is found.</response>
        /// <response code="400">If the request contains invalid data.</response>
        /// <response code="404">If the domain does not exist.</response>
        /// <response code="500">If an internal error has occurred.</response>
        /// <returns></returns>
        [HttpGet("{secondLevelDomain}.{topLevelDomain}/services")]
        [ProducesResponseType(typeof(DomainServiceListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DomainServiceListResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(DomainNotFoundResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        public async Task<IActionResult> ListServices(
            [FromServices] ApiProjectionsContext context,
            [FromRoute] string secondLevelDomain,
            [FromRoute] string topLevelDomain,
            CancellationToken cancellationToken = default)
        {
            var request = new ListServicesRequest
            {
                SecondLevelDomain = secondLevelDomain,
                TopLevelDomain = topLevelDomain,
            };

            await new ListServicesRequestValidator()
                .ValidateAndThrowAsync(request, cancellationToken: cancellationToken);

            var domain = await FindDomainAsync(context, secondLevelDomain, topLevelDomain, cancellationToken);

            return Ok(
                new DomainServiceListResponse(domain)
                {
                    Services = domain
                        .Services
                        .Select(x => new DomainServiceListItemResponse(domain, x))
                        .ToList()
                });
        }

        /// <summary>
        /// Get details of a domain service.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="secondLevelDomain">Second level domain of the domain to get details of a domain service for.</param>
        /// <param name="topLevelDomain">Top level domain of the domain to get details of a domain service for.</param>
        /// <param name="serviceId">Unique service id to get details for.</param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">If the domain and domain service is found.</response>
        /// <response code="400">If the request contains invalid data.</response>
        /// <response code="404">If the domain or domain service does not exist.</response>
        /// <response code="500">If an internal error has occurred.</response>
        /// <returns></returns>
        [HttpGet("{secondLevelDomain}.{topLevelDomain}/services/{serviceId}")]
        [ProducesResponseType(typeof(DomainServiceDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DomainServiceResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(ServiceNotFoundResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        public async Task<IActionResult> DetailService(
            [FromServices] ApiProjectionsContext context,
            [FromRoute] string secondLevelDomain,
            [FromRoute] string topLevelDomain,
            [FromRoute] Guid? serviceId,
            CancellationToken cancellationToken = default)
        {
            var request = new DetailServiceRequest
            {
                SecondLevelDomain = secondLevelDomain,
                TopLevelDomain = topLevelDomain,
            };

            await new DetailServiceRequestValidator()
                .ValidateAndThrowAsync(request, cancellationToken: cancellationToken);

            var service = await FindServiceAsync(context, serviceId, cancellationToken);

            await FindDomainAsync(context, secondLevelDomain, topLevelDomain, cancellationToken);

            return Ok(
                new DomainServiceDetailResponse(service));
        }

        /// <summary>
        /// Remove a domain service.
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="context"></param>
        /// <param name="commandId">Optional unique identifier for the request.</param>
        /// <param name="secondLevelDomain">Second level domain of the domain to remove the domain service from.</param>
        /// <param name="topLevelDomain">Top level domain of the domain to remove the domain service from.</param>
        /// <param name="serviceId">Unique service id to remove.</param>
        /// <param name="cancellationToken"></param>
        /// <response code="202">If the request has been accepted.</response>
        /// <response code="400">If the request contains invalid data.</response>
        /// <response code="404">If the domain or domain service does not exist.</response>
        /// <response code="500">If an internal error has occurred.</response>
        /// <returns></returns>
        [HttpDelete("{secondLevelDomain}.{topLevelDomain}/services/{serviceId}")]
        [ProducesResponseType(typeof(LastObservedPositionResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status202Accepted, typeof(LastObservedPositionResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(ServiceNotFoundResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples), jsonConverter: typeof(StringEnumConverter))]
        public async Task<IActionResult> RemoveService(
            [FromServices] ICommandHandlerResolver bus,
            [FromServices] ApiProjectionsContext context,
            [FromCommandId] Guid commandId,
            [FromRoute] string secondLevelDomain,
            [FromRoute] string topLevelDomain,
            [FromRoute] Guid? serviceId,
            CancellationToken cancellationToken = default)
        {
            var request = new RemoveServiceRequest
            {
                SecondLevelDomain = secondLevelDomain,
                TopLevelDomain = topLevelDomain,
                ServiceId = serviceId
            };

            // TODO: We can check in the eventstore if those aggregates even exist
            await new RemoveServiceRequestValidator()
                .ValidateAndThrowAsync(request, cancellationToken: cancellationToken);

            var command = RemoveServiceRequestMapping.Map(request);

            return Accepted(
                $"/v1/domains/{command.DomainName}/services",
                new LastObservedPositionResponse(
                    await bus.Dispatch(
                        commandId,
                        command,
                        GetMetadata(),
                        cancellationToken)));
        }

        private static async Task<DomainDetail> FindDomainAsync(
            ApiProjectionsContext context,
            string secondLevelDomain,
            string topLevelDomain,
            CancellationToken cancellationToken)
        {
            var domain = await context
                .DomainDetails
                .FindAsync(new object[] { $"{secondLevelDomain}.{topLevelDomain}" }, cancellationToken);

            if (domain == null)
                throw new ApiException(DomainNotFoundResponseExamples.Message, StatusCodes.Status404NotFound);

            return domain;
        }

        private static async Task<ServiceDetail> FindServiceAsync(
            ApiProjectionsContext context,
            Guid? serviceId,
            CancellationToken cancellationToken)
        {
            if (!serviceId.HasValue)
                throw new ApiException(ServiceNotFoundResponseExamples.Message, StatusCodes.Status404NotFound);

            var service = await context
                .ServiceDetails
                .FindAsync(new object[] { serviceId.Value }, cancellationToken);

            if (service == null)
                throw new ApiException(ServiceNotFoundResponseExamples.Message, StatusCodes.Status404NotFound);

            return service;
        }
    }
}
