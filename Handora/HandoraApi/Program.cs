using HandoraApi.Extensions;
using HandoraApplication;
using HandoraInfrastructure;
using OpenApiUi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllers()
    .AddDataAnnotationsLocalization(options => {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(HandoraApi.SharedResource));
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureDbContext(builder.Configuration);
builder.Services.ConfigureIdentity();
builder.Services.ConfigureAuthentication(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration).AddReposetoriesServices();
builder.Services.ConfigureCors();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.ConfigureRedis(builder.Configuration);

var app = builder.Build();

await app.InitialiseDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseOpenApiUi();
}

var supportedCultures = new[] { "en", "ar" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

app.UseCors();
app.UseCustomMiddlewares();

app.Run();
