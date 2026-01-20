using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Avatar_3D_Sentry.Swagger;

public sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formParams = context.ApiDescription.ParameterDescriptions
            .Where(param => param.Source == BindingSource.Form)
            .ToList();

        if (formParams.Count == 0)
        {
            return;
        }

        var containsFile = formParams.Any(IsFileParameter);
        if (!containsFile)
        {
            return;
        }

        var properties = new Dictionary<string, OpenApiSchema>();
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in formParams)
        {
            var schema = BuildSchema(param, context);
            properties[param.Name] = schema;

            if (param.IsRequired)
            {
                required.Add(param.Name);
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = required.Count > 0,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties,
                        Required = required.Count > 0 ? required : null
                    }
                }
            }
        };
    }

    private static bool IsFileParameter(ApiParameterDescription parameter)
    {
        var type = parameter.Type;
        if (typeof(IFormFile).IsAssignableFrom(type))
        {
            return true;
        }

        if (typeof(IEnumerable<IFormFile>).IsAssignableFrom(type))
        {
            return true;
        }

        return false;
    }

    private static OpenApiSchema BuildSchema(ApiParameterDescription parameter, OperationFilterContext context)
    {
        if (IsFileParameter(parameter))
        {
            return new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            };
        }

        var modelType = parameter.ModelMetadata?.ModelType ?? parameter.Type;
        return context.SchemaGenerator.GenerateSchema(modelType, context.SchemaRepository);
    }
}
