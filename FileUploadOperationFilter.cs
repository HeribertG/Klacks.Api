using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Klacks_api;

public class FileUploadOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var fileUploadMime = "multipart/form-data";

    // Initialize RequestBody if it's null
    if (operation.RequestBody == null)
    {
      operation.RequestBody = new OpenApiRequestBody
      {
        Content = new Dictionary<string, OpenApiMediaType>()
      };
    }

    // Check if multipart/form-data exists or create it
    if (!operation.RequestBody.Content.Any(x => x.Key.Equals(fileUploadMime, StringComparison.InvariantCultureIgnoreCase)))
    {
      operation.RequestBody.Content.Add(fileUploadMime, new OpenApiMediaType
      {
        Schema = new OpenApiSchema
        {
          Type = "object",
          Properties = new Dictionary<string, OpenApiSchema>()
        }
      });
    }

    // Add IFormFile parameters to the schema
    var fileParams = context.MethodInfo.GetParameters().Where(p => p.ParameterType == typeof(IFormFile));
    foreach (var param in fileParams)
    {
      operation.RequestBody.Content[fileUploadMime].Schema.Properties.Add(param.Name ?? "", new OpenApiSchema
      {
        Type = "string",
        Format = "binary"
      });
    }
  }
}
