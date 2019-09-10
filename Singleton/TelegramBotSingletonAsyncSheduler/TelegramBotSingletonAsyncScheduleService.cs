using AbstractAsyncScheduler;
using Microsoft.Extensions.Logging;

namespace TelegramBotSingletonAsyncSheduler
{
    public class TelegramBotSingletonAsyncScheduleService : BasicSingletonScheduler
    {
        public string TelegramBotApiKey { get; set; }

        public TelegramBotSingletonAsyncScheduleService(ILoggerFactory loggerFactory, string set_telegram_bot_api_key) : base(loggerFactory)
        {

        }

        protected override void AsyncScheduleAction()
        {
            // throw new System.NotImplementedException();
        }
    }
}
