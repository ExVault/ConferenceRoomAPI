using ConferenceRoomAPI.Common.Types;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ConferenceRoomAPI.Common.OpenApi;

public class StrictDateTimeOffsetTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken ct)
    {
        if (context.JsonTypeInfo.Type == typeof(StrictDateTimeOffset) ||
            context.ParameterDescription?.Type == typeof(StrictDateTimeOffset))
        {
            schema.Format = "date-time";
            
            schema.Description = "RFC 3339 timestamp with an explicit UTC offset, " +
                                 "such as 2026-09-15T10:00:00+03:00 or 2026-09-15T07:00:00Z.";
        }

        return Task.CompletedTask;
    }
}
