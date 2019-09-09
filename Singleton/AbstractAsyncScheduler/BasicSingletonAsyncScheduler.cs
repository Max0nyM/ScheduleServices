using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace AbstractAsyncScheduler
{
    public abstract class BasicSingletonScheduler
    {
        #region настройки/состояние планировщика

        /// <summary>
        /// Разрешённая длительность (в секундах) планировщику на выполнение задачи
        /// </summary>
        public int TimeoutBusySchedule { get; private set; } = 5;

        /// <summary>
        /// Максимальный размер стека трассировки статуса планировщика
        /// </summary>
        public int MaximumSizeSchedulerStatusTraceStack { get; private set; } = 100;

        /// <summary>
        /// Максимальный срок хранения (в секундах) записи в стеке трассировки статуса планировщика
        /// </summary>
        public int MaximumLifetimeSchedulerStatusTrace { get; private set; } = 60 * 1000;

        /// <summary>
        /// Паузы между выполнениями команды. Будет считаться с момента перехода планировщика в режим готовности
        /// </summary>
        public int SchedulePausePeriod;

        /// <summary>
        /// История состояний планировщика
        /// </summary>
        public ConcurrentBag<TracertItemModel> TracertChangeStatus { get; private set; } = new ConcurrentBag<TracertItemModel>();

        /// <summary>
        /// Дата/Время последнего изменения состояния
        /// </summary>
        public DateTime LastChangeStatusDateTime
        {
            get
            {
                DateTime max_date = DateTime.MinValue;

                lock (TracertChangeStatus)
                {
                    max_date = TracertChangeStatus.Max(x => x.DateCreate);
                }

                return max_date;
            }
        }

        /// <summary>
        /// Самая ранняя дата/время установки состояния
        /// </summary>
        public DateTime StartChangeStatusDateTime
        {
            get
            {
                DateTime min_date = DateTime.MinValue;
                lock (TracertChangeStatus)
                {
                    min_date = TracertChangeStatus.Where(x => x.DateCreate > DateTime.MinValue).Min(x => x.DateCreate);
                }
                return min_date;
            }
        }

        private string protected_Status = null;
        /// <summary>
        /// Состояние планировщика
        /// </summary>
        public string ScheduleStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(protected_Status))
                    return null;

                int life_period = (LastChangeStatusDateTime.AddSeconds(TimeoutBusySchedule) - DateTime.Now).Seconds;
                if (protected_Status != null && life_period < 0)
                {
                    lock (TracertChangeStatus)
                    {
                        if (LastChangeStatusDateTime > DateTime.MinValue)
                        {
                            string log_error_message = "Для планировщика [" + GetType().FullName + "] - статус [" + protected_Status + "] просрочен на [" + (-life_period) + "] сек. Поэтому будет сброшен на NULL" + Environment.NewLine;

                            foreach (TracertItemModel item_trace in TracertChangeStatus.Where(x => x.IsAvailable))
                            {
                                log_error_message += "  *   " + item_trace.ToString() + Environment.NewLine;
                                item_trace.IsOff = true;
                            }

                            AppLogger.LogWarning(log_error_message);
                        }
                    }
                    SetStatus(null);
                }
                return protected_Status;
            }
        }

        public void SetStatus(string new_status, StatusTypes StatusType = StatusTypes.SetValueStatus)
        {
            if (string.IsNullOrWhiteSpace(new_status) && string.IsNullOrWhiteSpace(protected_Status))
                return;

            protected_Status = new_status;
            lock (TracertChangeStatus)
            {
                TracertChangeStatus.Add(new TracertItemModel() { DateCreate = DateTime.Now, Information = new_status, TypeTracert = StatusType, Id = TracertChangeStatus.Count + 1 });
                TracertChangeStatus = new ConcurrentBag<TracertItemModel>(TracertChangeStatus.OrderByDescending(x => x.DateCreate));

                if (TracertChangeStatus.Count() > MaximumSizeSchedulerStatusTraceStack + 10)
                    TracertChangeStatus = new ConcurrentBag<TracertItemModel>(TracertChangeStatus.Skip(TracertChangeStatus.Count() - MaximumSizeSchedulerStatusTraceStack));

                if (StartChangeStatusDateTime.AddSeconds(MaximumLifetimeSchedulerStatusTrace) < DateTime.Now)
                    TracertChangeStatus = new ConcurrentBag<TracertItemModel>(TracertChangeStatus.Where(x => x.DateCreate > DateTime.Now.AddSeconds(-MaximumLifetimeSchedulerStatusTrace)));
            }
        }


        /// <summary>
        /// Состояние планировщика
        /// </summary>
        public virtual bool SchedulerIsReady => string.IsNullOrEmpty(ScheduleStatus);

        #endregion

        private async void IntervalHandling()
        {
            await Task.Run(() =>
            {
                InvokeAsyncSchedule();
                System.Threading.Thread.Sleep(1000 * SchedulePausePeriod);
            });
        }

        protected ILogger AppLogger { get; set; }

        public BasicSingletonScheduler(ILoggerFactory loggerFactory)
        {
            AppLogger = loggerFactory.CreateLogger(GetType().Name + "Logger");
            IntervalHandling();
        }

        /// <summary>
        /// Асинхронный метод планировщика
        /// </summary>
        protected abstract void AsyncScheduleAction();
        private async void AsyncOperation()
        {
            SetStatus("Запуск плановой асинхронной операции [" + GetType().FullName + "]");
            await Task.Run(() =>
            {
                AsyncScheduleAction();
                SetStatus("Окончание плановой асинхронной операции");
                SetStatus(null);
            });
        }

        public void InvokeAsyncSchedule()
        {
            // Время не настало
            if (LastChangeStatusDateTime.AddSeconds(SchedulePausePeriod) > DateTime.Now)
            {
                AppLogger.LogTrace("Планировщик ожидает своего времени: " + GetType().Name);
                return;
            }
            // Планировщик занят
            if (!SchedulerIsReady)
            {
                AppLogger.LogWarning("Планировщик занят: " + GetType().Name);
                return;
            }
            try
            {
                AppLogger.LogInformation("Запуск планировщика по расписанию: " + GetType().Name);
                AsyncOperation();
            }
            catch (Exception e)
            {
                string err_msg = "Ошибка выполнения асинхронной операции планировщика: " + e.Message;
                SetStatus(err_msg, StatusTypes.ErrorStatus);
                AppLogger.LogError(err_msg);
                SetStatus(null);
            }
        }
    }
}