using System.Text;
using CleannetCodeBot.Twitch;
using CleannetCodeBot.Twitch.Controllers;
using CleannetCodeBot.Twitch.Infrastructure;
using CleannetCodeBot.Twitch.Polls;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
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
    ClientId = x.GetRequiredService<IOptions<AppSettings>>().Value.ClientId,
});

builder.Services.Configure<PollSettings>(
    builder.Configuration.GetSection(nameof(PollSettings)));

builder.Services.AddSingleton<ITwitchAPI, TwitchAPI>();

builder.Services.AddSingleton<IPollsRepository, PollsRepository>();
builder.Services.AddSingleton<IQuestionsRepository, QuestionsRepository>();
builder.Services.AddSingleton<IUsersPollStartRegistry, UsersPollStartRegistry>();

builder.Services.AddSingleton<IPollsService, PollsService>();

builder.Services.AddHostedService<TwitchWebsocketBackgroundService>();
builder.Services.AddHostedService<TwitchBotBackgroundService>();
builder.Services.AddHostedService<ScheduleBackgroundService>();

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

app.MapGet("/", (ITwitchAPI twitchApi, IOptions<AppSettings> appSettings, IMemoryCache memoryCache) =>
{
    var state = Guid.NewGuid().ToString("N");
    memoryCache.Set(AuthController.UserStateKey, state);
    var clientId = appSettings.Value.ClientId;
    var redirectUri = appSettings.Value.RedirectUri;

    var url = twitchApi.Auth.GetAuthorizationCodeUrl(
        redirectUri: redirectUri,
        scopes: new[]
        {
            AuthScopes.Chat_Read,
            AuthScopes.Chat_Edit,
            AuthScopes.Helix_Channel_Read_Polls,
            AuthScopes.Helix_Channel_Manage_Redemptions,
            AuthScopes.Helix_Channel_Read_Redemptions,
        },
        state: state,
        clientId: clientId);
    return Results.Extensions.Html(@$"<!doctype html>
<html>
    <head><title>miniHTML</title></head>
    <body>
        <h1>Hello World</h1>
        <p>The time on the server is {DateTime.Now:O}</p>
        <a href=""{url}"">Twitch login</a>
    </body>
</html>");
});

app.MapControllers();

app.Run();