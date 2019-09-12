////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace AbstractAsyncScheduler
{
    public abstract class BasicSingletonScheduler
    {
        // TODO: Реализовать принудительную/ручную остановку/запуск асинхронного потока. Изначально в конструкторе определён автозапуск неуправляемого потока

        #region настройки/состояние планировщика

        /// <summary>
        /// Максимальный размер транзитных данных
        /// </summary>
        public int MaxSizeTransit { get; protected set; } = 50;

        /// <summary>
        /// Разрешённая длительность (в секундах) планировщику на выполнение задачи
        /// </summary>
        public int TimeoutBusySchedule { get; private set; } = 5;

        /// <summary>
        /// Максимальный размер стека трассировки статуса планировщика
        /// </summary>
        public int MaximumSizeSchedulerStatusTraceStack { get; private set; } = 100; // максимальный размер хранилища логов 100 строк

        /// <summary>
        /// Максимальный срок хранения (в секундах) записи в стеке трассировки статуса планировщика
        /// </summary>
        public int MaximumLifetimeSchedulerStatusTrace { get; private set; } = 60 * 60 * 3; // максимальный срок хранения логов 3 часа

        /// <summary>
        /// Паузы между выполнениями команды. Будет считаться с момента перехода планировщика в режим готовности
        /// </summary>
        public int SchedulePausePeriod;

        /// <summary>
        /// Признак того что настало время запустить обработку 
        /// </summary>
        public bool ItTimeToRunProcessing => LastChangeStatusDateTime.AddSeconds(SchedulePausePeriod) >= DateTime.Now;

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
                if (TracertChangeStatus.Count() > 0)
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
            {
                AppLogger.LogTrace("Попытка установить статус null на null. Проигнорировано.");
                return;
            }

            switch (StatusType)
            {
                case StatusTypes.SetValueStatus:
                    AppLogger.LogWarning(new_status);
                    break;
                case StatusTypes.ErrorStatus:
                    AppLogger.LogError(new_status);
                    break;
                default:
                    AppLogger.LogCritical("Тип статуса [" + StatusType.ToString() + "] за пределами доступных значений: " + new_status);
                    break;
            }

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
        /// Состояние планировщика. Зависит от текущего статуса планировщика, который в свою очередь устанавливается методом SetStatus(string new_status, StatusTypes StatusType = StatusTypes.SetValueStatus).
        /// Планировщик свободен если текущий статус string.IsNullOrEmpty(ScheduleStatus) или время его установки устарело по таймауту TimeoutBusySchedule.
        /// Если текущий статус не пустая строка и не NULL и вместе с тем значение не просрочено по таймауту TimeoutBusySchedule, то считается, что планировщик занят и не должен начинать следующую операцию.
        /// Примечание: при назначении какого либо значимого статуса (не NULL) планировщику фиксируется время назначения этого статуса. И если значение не NULL и ещё не зависло (по версии параметра таймаута TimeoutBusySchedule), то выполнение команд планировщик будет отклонять.
        /// Если выполнение одной операции зависает более чем на TimeoutBusySchedule, то планировщику автоматически принудительно ставится состояние NULL, что в свою очередь будет сигнализировать о том что планировщик готов выполнить новую операцию
        /// </summary>
        public virtual bool SchedulerIsReady => string.IsNullOrEmpty(ScheduleStatus);

        #endregion

        private async void IntervalHandling()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    InvokeAsyncSchedule();
                    System.Threading.Thread.Sleep(1000 * SchedulePausePeriod);
                }
            });
        }

        private ILogger AppLogger { get; set; }

        public BasicSingletonScheduler(ILoggerFactory loggerFactory, int set_schedule_pause_period)
        {
            SchedulePausePeriod = set_schedule_pause_period;
            AppLogger = loggerFactory.CreateLogger(GetType().Name + "Logger");
            SetStatus("Инициализация " + GetType().Name);
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
                try
                {
                    AsyncScheduleAction();
                    SetStatus("Окончание плановой асинхронной операции");
                }
                catch (Exception e)
                {
                    SetStatus("Выполнение асинхронной задачи завершилось ошибкой: " + e.Message);
                }
                SetStatus(null);
            });
        }

        public virtual void InvokeAsyncSchedule()
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
                SetStatus("Ошибка выполнения асинхронной операции планировщика: " + e.Message, StatusTypes.ErrorStatus);
                SetStatus(null);
            }
        }
    }
}