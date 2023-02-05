using Telegram.Bot.Types.Enums;

namespace CleannetCode_bot
{
    public class HandlersMap
    {
        // private readonly Dictionary<UpdateType, IUpdateHandler[]> _map;
        private readonly Dictionary<UpdateType, Func<IUpdateHandler[]>> _map;

        public HandlersMap(Dictionary<UpdateType, Func<IUpdateHandler[]>> map)
        {
            _map = map;
        }

        public IUpdateHandler[] GetHandlers(UpdateType updateType)
        {
            if (!_map.TryGetValue(updateType, out var handlersGenerator))
            {
                return Array.Empty<IUpdateHandler>();
            }

            return handlersGenerator().ToArray();
        }
    }
}