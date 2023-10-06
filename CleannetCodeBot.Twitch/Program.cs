using CleannetCodeBot.Twitch;
using TwitchLib.Client;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.EventSub.Websockets.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTwitchLibEventSubWebsockets();
builder.Services.AddHostedService<WebsocketHostedService>();
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();