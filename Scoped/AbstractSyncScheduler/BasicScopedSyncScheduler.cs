////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using AbstractAsyncScheduler;
using Microsoft.EntityFrameworkCore;

namespace AbstractSyncScheduler
{
    public abstract class BasicScopedSyncScheduler
    {
        protected DbContext db;
        protected virtual BasicSingletonScheduler BasicSingletonService { get; set; }
        public virtual bool IsReady => BasicSingletonService.SchedulerIsReady;

        public BasicScopedSyncScheduler(DbContext set_db, BasicSingletonScheduler set_basic_singleton_service)
        {
            db = set_db;
            BasicSingletonService = set_basic_singleton_service;
            /*
             if (IsReady)
            {
                BasicSingletonService.SetStatus("Запуск sync scoped service", StatusTypes.DebugStatus);
                SyncUpdate();
                BasicSingletonService.SetStatus(null, StatusTypes.DebugStatus);
            }
             */

        }



        /// <summary>
        /// Метод записи данных в базу данных из асинхронного singleton сервиса
        /// </summary>
        public abstract void SyncUpdate();
    }
}
