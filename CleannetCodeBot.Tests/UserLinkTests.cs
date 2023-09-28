using System.Text;
using CleannetCodeBot.Core;

namespace CleannetCodeBot.Tests;

public class UserLinkTests
{
    [Fact]
    public void NewUserLink_ShouldReturnText()
    {
        var message = new UserLink("Test text", 0);
        Assert.NotNull(message?.Value);
        Assert.NotEmpty(message.Value);
    }

    [Fact]
    public void NewUserLink_InsertSpecialCharacter_ShouldReturnText()
    {
        var specialCharacters = UserLink.SpecialCharacters;

        foreach (var specialCharacter in specialCharacters)
        {
            var text = "test";
            var randomIndex = Random.Shared.Next(0, text.Length);
            text = new StringBuilder(text[0..randomIndex])
                .Append(specialCharacter)
                .Append(text[randomIndex..text.Length])
                .ToString();

            var message = new UserLink(text, 0);
            Assert.Contains("\\", message.Value);
        }
    }
}