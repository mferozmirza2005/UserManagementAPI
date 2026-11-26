using Microsoft.OpenApi;
using UserManagementAPI.Middleware;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TechHive Solutions User Management API",
        Version = "v1",
        Description = "Internal API for managing users across TechHive Solutions applications. Send Authorization: Bearer {token}."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token-based authentication. Example: Bearer techhive-dev-token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Token"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IUserService, UserService>();

var app = builder.Build();

// Custom middleware order (assignment): error handling first, authentication next, logging last.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1");
        options.RoutePrefix = "swagger";
    });

    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseMiddleware<TokenAuthenticationMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
