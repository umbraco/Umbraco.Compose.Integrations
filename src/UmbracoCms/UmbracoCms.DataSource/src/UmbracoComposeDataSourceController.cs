using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Compose.Integrations.UmbracoCms.DataSource;

/// <summary>
/// The back office, or management api controller providing implementation of content querying for the Umbraco Compose Content Picker.
/// </summary>
/// <remarks>
/// The primary constructor for the Umbraco Compose Data Source Controller.
/// </remarks>
/// <param name="dataTypeService">The service required to look up the data type from the id argument provided to all endpoints</param>
/// <param name="graphQlContentQueryService">The service required to translate the data type configuration in to graphql queries and execute those queries</param>
/// <param name="logger">The logger for any warning messages</param>
[VersionedApiBackOfficeRoute("compose/data-source")]
[ApiExplorerSettings(GroupName = Constants.ApiGroupName)]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
[MapToApi(Constants.DataSourceApiName)]
public sealed class UmbracoComposeDataSourceController(
    IDataTypeService dataTypeService,
    IGraphQlContentQueryService graphQlContentQueryService,
    ILogger<UmbracoComposeDataSourceController> logger) : ManagementApiControllerBase
{
    private readonly ILogger<UmbracoComposeDataSourceController> _logger = logger;
    private readonly IDataTypeService _dataTypeService = dataTypeService;
    private readonly IGraphQlContentQueryService _graphQlContentQueryService = graphQlContentQueryService;

    /// <summary>
    /// The endpoint to look up content items for the Umbraco Compose Content Picker data source.
    /// </summary>
    /// <param name="dataTypeId">The id of the data type as sourced from the Umbraco instance database</param>
    /// <param name="take">The maximum number of content item to return</param>
    /// <param name="afterCursor">Take result items from the first content item after this cursor</param>
    /// <param name="searchTerm">The optional search term to provide to a {searchfield}_contains graphql filter</param>
    /// <returns>The successful or error action ressult, including content items or an error message</returns>
    [HttpGet]
    public async Task<IActionResult> GetContentItemsAsync(
        [FromQuery] string dataTypeId,
        [FromQuery] int take = 50,
        [FromQuery] string? afterCursor = null,
        [FromQuery] string? searchTerm = null)
    {
        bool isGuid = Guid.TryParse(dataTypeId, out Guid parsedGuid);

        if (!isGuid)
        {
            return BadRequest($"Invalid argument {nameof(dataTypeId)}. Expected a Guid, but provided {dataTypeId}");
        }

        Cms.Core.Models.IDataType? dataType = await _dataTypeService.GetAsync(parsedGuid).ConfigureAwait(false);
        if (dataType is null)
        {
            return BadRequest($"{dataTypeId} is not a valid data type identifier");
        }

        try
        {
            UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments = new(dataType);
            UmbracoComposeContentPickerDataSourcePaging paging = new(afterCursor, take);
            ContentQueryResult result = await _graphQlContentQueryService.GetContentAsync(composeQueryArguments, paging, searchTerm)
                .ConfigureAwait(false);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while getting content items for datatype: {DataTypeId} with search term {SearchTerm}",
                dataTypeId,
                searchTerm ?? "(none)");
            return BadRequest("An error occurred while processing the data type configuration");
        }
    }

    /// <summary>
    /// The endpoint to look up specific content items for the Umbraco Compose Content Picker data source by their keys.
    /// </summary>
    /// <param name="dataTypeId">The id of the data type as sourced from the Umbraco instance database</param>
    /// <param name="keys">The keys of the content items to look up with a {keyfield}_any graphql query</param>
    /// <returns>The successful or error action ressult, including content items or an error message</returns>
    [HttpGet("contentItems")]
    public async Task<IActionResult> GetContentItemsByIdAsync(
        [FromQuery] string dataTypeId,
        [FromQuery] string[] keys)
    {
        if (keys.Length == 0)
        {
            return BadRequest("No keys provided when looking up content items");
        }

        bool isGuid = Guid.TryParse(dataTypeId, out Guid parsedGuid);

        if (!isGuid)
        {
            return BadRequest($"Invalid argument {nameof(dataTypeId)}. Expected a Guid, but provided {dataTypeId}");
        }

        Cms.Core.Models.IDataType? dataType = await _dataTypeService.GetAsync(parsedGuid).ConfigureAwait(false);
        if (dataType is null)
        {
            return BadRequest($"{dataTypeId} is not a valid data type identifier");
        }
        try
        {
            UmbracoComposeContentPickerDataSourceConfiguration composeQueryArguments = new(dataType);
            ContentQueryResult result = await _graphQlContentQueryService.GetContentItemsAsync(composeQueryArguments, keys)
                .ConfigureAwait(false);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting identified content items for datatype: {DataTypeId}", dataTypeId);
            return BadRequest("An error occurred while processing the data type configuration");
        }
    }
}
