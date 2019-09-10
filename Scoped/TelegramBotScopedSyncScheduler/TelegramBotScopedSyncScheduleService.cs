using AbstractSyncScheduler;
using Microsoft.EntityFrameworkCore;
using TelegramBotSingletonAsyncSheduler;

namespace TelegramBotScopedSyncScheduler
{
    public class TelegramBotScopedSyncScheduleService : BasicScopedSyncScheduler
    {
        public TelegramBotSingletonAsyncScheduleService AsyncTelegramBotScheduleService { get; private set; }

        public TelegramBotScopedSyncScheduleService(DbContext set_db, TelegramBotSingletonAsyncScheduleService set_async_telegram_bot_schedule_service) : base(set_db)
        {
            AsyncTelegramBotScheduleService = set_async_telegram_bot_schedule_service;


        }

        public override void UpdateDataBase()
        {
            //throw new NotImplementedException();
        }
    }
}
