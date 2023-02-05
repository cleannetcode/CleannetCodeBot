using Telegram.Bot.Types.Enums;

namespace CleannetCode_bot
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SupportedUpdateType : Attribute
    {
        public SupportedUpdateType(UpdateType updateType)
        {
            UpdateType = updateType;
        }

        public UpdateType UpdateType { get; }
    }
}