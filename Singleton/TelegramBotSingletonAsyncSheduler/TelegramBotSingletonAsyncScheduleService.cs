using AbstractAsyncScheduler;
using Microsoft.Extensions.Logging;

namespace TelegramBotSingletonAsyncSheduler
{
    public class TelegramBotSingletonAsyncScheduleService : BasicSingletonScheduler
    {
        public string TelegramBotApiKey { get; set; }

        public TelegramBotSingletonAsyncScheduleService(ILoggerFactory set_logger_factory, string set_telegram_bot_api_key, int set_schedule_pause_period) 
            : base(set_logger_factory, set_schedule_pause_period)
        {

        }

        protected override void AsyncScheduleAction()
        {
            // throw new System.NotImplementedException();
        }
    }
}
