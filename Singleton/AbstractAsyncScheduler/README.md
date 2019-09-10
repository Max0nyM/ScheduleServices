## abstract base Scheduler

Базовый функционал для асинхронных Singleton сервисов.

Асинхронные сервисы:
- [Electrum](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/ElectrumSingletonAsyncSheduler)
- [LocalBitcoins](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/LocalbitcoinsBtcRateSingletonAsyncScheduler)
- [TelegramBot](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/TelegramBotSingletonAsyncSheduler)

наследуются от этого Singleton сервиса.

- таймаут на выполнение операции `TimeoutBusySchedule` (по умолчинию 5 секунд)
- сервис ведёт подробный лог работы. Достп к логам открыт (`TracertChangeStatus`). Размер и срок хранения логов определяются в параметрах `MaximumSizeSchedulerStatusTraceStack` и `MaximumLifetimeSchedulerStatusTrace`