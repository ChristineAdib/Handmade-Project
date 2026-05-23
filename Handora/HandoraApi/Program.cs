using HandoraApi.Extensions;
using HandoraApplication;
using HandoraInfrastructure;
using HandoraInfrastructure.Seeders;
using Microsoft.AspNetCore.Identity;
using OpenApiUi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureDbContext(builder.Configuration);
builder.Services.ConfigureIdentity();
builder.Services.ConfigureAuthentication(builder.Configuration);
builder.Services.AddInfrastructureServices().AddReposetoriesServices();
builder.Services.ConfigureCors();
builder.Services.AddApplicationServices();

var app = builder.Build();

await app.InitialiseDatabaseAsync();

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await RoleSeeder.SeedAsync(roleManager);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseOpenApiUi();
}

app.UseCors();
app.UseCustomMiddlewares();

app.Run();
