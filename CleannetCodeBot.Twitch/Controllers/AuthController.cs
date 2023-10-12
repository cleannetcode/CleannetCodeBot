using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CleannetCodeBot.Twitch.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthController> _logger;
    private readonly AppSettings _appSettings;
    private const string UserStateKey = "user_state";

    public AuthController(
        IMemoryCache memoryCache,
        IOptions<AppSettings> options,
        HttpClient httpClient,
        ILogger<AuthController> logger)
    {
        _memoryCache = memoryCache;
        _httpClient = httpClient;
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
        var url = new Uri("https://id.twitch.tv/oauth2/token");

        var body = new StringBuilder()
            .Append("client_id=").Append(clientId).Append('&')
            .Append("client_secret=").Append(clientSecret).Append('&')
            .Append("code=").Append(code).Append('&')
            .Append("grant_type=authorization_code").Append('&')
            .Append("redirect_uri=").Append(redirectUri)
            .ToString();

        var stringContent = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await _httpClient.PostAsync(url, stringContent, cancellationToken);
        if (response.IsSuccessStatusCode == false)
        {
            _logger.LogError("Failed to get auth token");
            _memoryCache.Remove(AuthToken.Key);
            return BadRequest("Failed to get auth token");
        }

        var authToken = await response.Content.ReadFromJsonAsync<AuthToken>(cancellationToken: cancellationToken);
        if (authToken == null)
        {
            _logger.LogError("Cannot deserialize auth token");
            _memoryCache.Remove(AuthToken.Key);
            return BadRequest("Cannot deserialize auth token");
        }

        _memoryCache.Set(AuthToken.Key, authToken);

        return Ok();
    }

    [HttpGet("code")]
    public IActionResult RequestCode()
    {
        var state = Guid.NewGuid().ToString("N");
        _memoryCache.Set(UserStateKey, state);

        var clientId = _appSettings.ClientId;
        var redirectUri = _appSettings.RedirectUri;

        var url = new StringBuilder("https://id.twitch.tv/oauth2/authorize")
            .Append('?')
            .Append("response_type=code").Append('&')
            .Append("client_id=").Append(clientId).Append('&')
            .Append("redirect_uri=").Append(redirectUri).Append('&')
            .Append("scope=channel%3Aread%3Apolls")
            .Append("+chat%3Aread")
            .Append("+chat%3Aedit")
            .Append("+channel%3Amanage%3Aredemptions")
            .Append("+channel%3Aread%3Aredemptions").Append('&')
            .Append("state=").Append(state)
            .ToString();

        return Ok(url);
    }
}