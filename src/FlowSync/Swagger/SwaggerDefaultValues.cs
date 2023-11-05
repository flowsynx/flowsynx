using FlowSync.Core.Serialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace FlowSync.Swagger;

/// <summary>
///   Represents the OpenAPI/Swashbuckle operation filter used to document information provided, but
///   not used.
/// </summary>
/// <remarks>
///   This <see cref="IOperationFilter"/> is only required due to bugs in the <see
///   cref="SwaggerGenerator"/>. Once they are fixed and published, this class can be removed.
/// </remarks>
public class SwaggerDefaultValues : IOperationFilter
{
    /// <inheritdoc/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        //if (operation.Parameters == null)
        //{
        //    return;
        //}
        
        //foreach (var parameter in operation.Parameters)
        //{
        //    var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

        //    parameter.Description ??= description.ModelMetadata?.Description;

        //    if (parameter.Schema.Default == null &&
        //         description.DefaultValue != null &&
        //         description.DefaultValue is not DBNull &&
        //         description.ModelMetadata is ModelMetadata modelMetadata)
        //    {
        //        var json = JsonSerializer.Serialize(description.DefaultValue, modelMetadata.ModelType);
        //        parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
        //    }

        //    parameter.Required |= description.IsRequired;
        //}
    }
}