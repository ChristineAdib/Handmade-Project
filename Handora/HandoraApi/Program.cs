using HandoraApi.Extensions;
using HandoraApplication;
using HandoraInfrastructure;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseOpenApiUi();
}

app.UseCors();
app.UseCustomMiddlewares();

app.Run();





// using System.Text;
// using HandoraInfrastructure.Data;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.IdentityModel.Tokens;
// using OpenApiUi;

// var builder = WebApplication.CreateBuilder(args);


// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


// // Add services to the container.

// // MapsterSettings.Configure();


// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(connectionString));
// // .EnableDetailedErrors(true), ServiceLifetime.Scoped
// builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
//             {
//                 // Configure password options
//                 options.Password.RequireDigit = false;
//                 options.Password.RequireLowercase = false;
//                 options.Password.RequireNonAlphanumeric = false;
//                 options.Password.RequireUppercase = false;
//                 options.Password.RequiredLength = 6;
//                 options.Password.RequiredUniqueChars = 0;
//             })
//             .AddEntityFrameworkStores<AppDbContext>()
//             .AddDefaultTokenProviders();


// builder.Services.AddAuthentication(options =>
//                 {
//                 options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//                 options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//                 options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
//                 options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
//             }
//             ).AddJwtBearer(o =>
//             {
//                 o.IncludeErrorDetails = true;
//                 o.RequireHttpsMetadata = false;
//                 o.SaveToken = false;
//                 o.TokenValidationParameters = new TokenValidationParameters
//                 {
//                     ValidateIssuerSigningKey = true,
//                     ValidateAudience = true,
//                     ValidateIssuer = true,
//                     ValidateLifetime = true,
//                     ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
//                     ValidAudience = builder.Configuration["JWT:ValidAudience"],
//                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
//                     ClockSkew = TimeSpan.Zero
//                 };
//             });




// builder.Services.AddControllers();
// // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();



// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("development",
//         policy =>
//         {
//             policy
//                 .AllowAnyOrigin()
//                 .AllowAnyMethod()
//                 .AllowAnyHeader();
//         });
//     options.AddPolicy("production",
//         policy =>
//         {
//             policy
//                 .AllowAnyOrigin()
//                 .AllowAnyMethod()
//                 .AllowAnyHeader();
//         });
// });


// var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseOpenApiUi();
// }

// app.UseCors("development");
// app.UseHttpsRedirection();
// app.UseStaticFiles();
// app.UseAuthentication();
// app.UseAuthorization();
// app.MapControllers();
// app.Run();
