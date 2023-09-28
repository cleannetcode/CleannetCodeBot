namespace CleannetCodeBot.Core;

public class UserLink
{
    public const string SpecialCharacters = @"_*[]()~`>#+-={}.!";

    public UserLink(string username, long userId)
    {
        var cleanUserName = EscapeSpecialCharacters(username);
        Value = $"[@{cleanUserName}](tg://user?id={userId})";
    }

    public string Value { get; }

    public static string EscapeSpecialCharacters(string input)
    {
        // https://core.telegram.org/bots/api#markdownv2-style
        var specialCharacters = new[]
        {
            "_", "*", "[", "]", "(", ")", "~", "`", ">",
            "#", "+", "-", "=", "|", "{", "}", ".", "!"
        };
        
        foreach (var specialCharacter in specialCharacters)
        {
            input = input.Replace(specialCharacter, "\\" + specialCharacter);
        }
        
        return input;
    }
}