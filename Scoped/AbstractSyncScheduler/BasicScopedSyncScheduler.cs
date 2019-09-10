﻿using AbstractAsyncScheduler;
using Microsoft.EntityFrameworkCore;

namespace AbstractSyncScheduler
{
    public abstract class BasicScopedSyncScheduler
    {
        protected DbContext db;
        protected BasicSingletonScheduler BasicSingletonService;
        public virtual bool IsReady => BasicSingletonService.SchedulerIsReady;

        public BasicScopedSyncScheduler(DbContext set_db, BasicSingletonScheduler set_basic_singleton_service)
        {
            db = set_db;
            BasicSingletonService = set_basic_singleton_service;

            if (IsReady)
                UpdateDataBase();
        }

        /// <summary>
        /// Метод записи данных в базу данных из асинхронного singleton сервиса
        /// </summary>
        public abstract void UpdateDataBase();
    }
}