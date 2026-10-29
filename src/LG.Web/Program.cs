using System.Text.Json.Serialization;
using LG.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GestionnaireParties>();
builder.Services.AddSingleton<Diffuseur>();
builder.Services.AddHostedService<HorlogeParties>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHub<PartieHub>("/hub/partie");

app.Run();
