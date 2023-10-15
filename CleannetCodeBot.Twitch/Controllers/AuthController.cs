using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TwitchLib.Api.Interfaces;

namespace CleannetCodeBot.Twitch.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly ITwitchAPI _twitchApi;
    private readonly ILogger<AuthController> _logger;
    private readonly AppSettings _appSettings;
    public const string UserStateKey = "user_state";

    public AuthController(
        IMemoryCache memoryCache,
        ITwitchAPI twitchApi,
        IOptions<AppSettings> options,
        ILogger<AuthController> logger)
    {
        _memoryCache = memoryCache;
        _twitchApi = twitchApi;
        _logger = logger;
        _appSettings = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> SetCode(
        [FromQuery] string code,
        [FromQuery] string scope,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        var userState = _memoryCache.Get<string>(UserStateKey);
        if (userState != state)
        {
            return BadRequest("invalid state");
        }

        var clientId = _appSettings.ClientId;
        var redirectUri = _appSettings.RedirectUri;
        var clientSecret = _appSettings.ClientSecret;

        var response = await _twitchApi.Auth.GetAccessTokenFromCodeAsync(
            code: code,
            clientSecret: clientSecret,
            redirectUri: redirectUri,
            clientId: clientId);
        if (response == null)
        {
            var message = "Cannot deserialize auth token";
            _logger.LogError(message);
            _memoryCache.Remove(AuthToken.Key);
            return BadRequest(message);
        }

        var authResponse = await _twitchApi.Auth.ValidateAccessTokenAsync(response.AccessToken);
        if (authResponse == null)
        {
            var message = "Access token is invalid";
            _logger.LogError(message);
            _memoryCache.Remove(AuthToken.Key);
            return BadRequest(message);
        }

        var usersResponse = await _twitchApi.Helix.Users.GetUsersAsync(
            ids: new List<string> { authResponse.UserId },
            accessToken: response.AccessToken);
        if (usersResponse.Users.Any(x => x.Login != "cleannetcode"))
        {
            var message = "User is not CleannetCode";
            _logger.LogError(message);
            _memoryCache.Remove(AuthToken.Key);
            return BadRequest(message);
        }

        var authToken = new AuthToken(response.AccessToken, response.ExpiresIn, response.RefreshToken);
        _memoryCache.Set(AuthToken.Key, authToken);
        return Ok();
    }
}