using Microsoft.EntityFrameworkCore;

namespace AbstractSyncScheduler
{
    public abstract class BasicScopedSyncScheduler
    {
        protected DbContext db;

        public BasicScopedSyncScheduler(DbContext set_db)
        {
            db = set_db;
        }

        /// <summary>
        /// Метод записи данных в базу данных из асинхронного singleton сервиса
        /// </summary>
        public abstract void UpdateDataBase();
    }
}
