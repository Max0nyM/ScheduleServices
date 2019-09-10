////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using AbstractAsyncScheduler;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TelegramBot.TelegramMetadata;
using TelegramBot.TelegramMetadata.AvailableTypes;
using TelegramBot.TelegramMetadata.GettingUpdates;

namespace TelegramBotSingletonAsyncSheduler
{
    public class TelegramBotSingletonAsyncScheduleService : BasicSingletonScheduler
    {
        public string TelegramBotApiKey { get; set; }

        public TelegramClientCore TelegramClient { get; set; }

        /// <summary>
        /// Транзитный набор полученных обновлений от TelegramBot 
        /// </summary>
        public ConcurrentBag<Update> TelegramBotUpdates { get; private set; } = new ConcurrentBag<Update>();

        /// <summary>
        /// Объект User, представляющий TelegramBot
        /// </summary>
        public UserClass TelegramBotUser { get; private set; }

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
