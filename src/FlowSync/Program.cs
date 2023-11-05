using Asp.Versioning;
using FlowSync.Core.Extensions;
using FlowSync.Endpoints.List;
using FlowSync.Extensions;
using FlowSync.Infrastructure.Extensions;
using FlowSync.Persistence.Json.Extensions;
using FluentValidation;
using IValidator = FlowSync.Core.Services.IValidator;

const string swaggerRoutePrefix = "api-docs";

var builder = WebApplication.CreateBuilder(args);

var version1 = new ApiVersion(1, 0);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLocation();
builder.Services.AddFlowSyncApplication();
builder.Services.AddFlowSyncInfrastructure();
builder.Services.AddFlowSyncJsonPersistence();
builder.Services.AddAndConfigApiVersioning(version1);
builder.Services.AddValidatorsFromAssemblyContaining<IValidator>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAndConfigSwagger();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => { options.RouteTemplate = $"{swaggerRoutePrefix}/{{documentName}}/docs.json"; });
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = swaggerRoutePrefix;
        foreach (var description in app.DescribeApiVersions())
            options.SwaggerEndpoint($"/{swaggerRoutePrefix}/{description.GroupName}/docs.json", description.GroupName.ToUpperInvariant());
    });
}

app.ConfigureCustomException();
app.UseHttpsRedirection();

var routing = app.NewVersionedApi("FlowSync").HasApiVersion(version1);
routing.MapList();

app.Run();