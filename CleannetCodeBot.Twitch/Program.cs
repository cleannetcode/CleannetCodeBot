using System.Text;
using CleannetCodeBot.Twitch;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Interfaces;
using TwitchLib.Client;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.EventSub.Websockets.Extensions;

Console.OutputEncoding = Encoding.Unicode;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRetryResiliencePipeline();
builder.Services.AddTwitchLibEventSubWebsockets();
builder.Services.AddSingleton<IApiSettings>(x => new ApiSettings
{
    ClientId = x.GetRequiredService<IOptions<AppSettings>>().Value.ClientId
});
builder.Services.AddSingleton<ITwitchAPI, TwitchAPI>();

builder.Services.AddHostedService<TwitchWebsocketBackgroundService>();
builder.Services.AddHostedService<TwitchBotBackgroundService>();

builder.Services.AddHttpClient();

builder.Services.AddMemoryCache();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<AppSettings>()
    .BindConfiguration("AppSettings");

builder.Services.AddSingleton<TwitchClient>(_ =>
{
    var clientOptions = new ClientOptions
    {
        MessagesAllowedInPeriod = 750,
        ThrottlingPeriod = TimeSpan.FromSeconds(30)
    };
    var customClient = new WebSocketClient(clientOptions);
    return new TwitchClient(customClient);
});

builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(x => x.AllowAnyOrigin());

app.UseAuthorization();

app.MapControllers();

app.Run();