## abstract base Singleton Service (aka Scheduler)

Базовый функционал для асинхронных Singleton сервисов. Можно внедрять прямо в проект (если не требуется запись в БД) или через Scoped сервисы-адапторы

Асинхронные сервисы:
- [LocalBitcoins](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/LocalbitcoinsBtcRateSingletonAsyncScheduler) **beta version**
- [Electrum](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/ElectrumSingletonAsyncSheduler) **beta version**
- [TelegramBot](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/TelegramBotSingletonAsyncSheduler) **beta version**

наследуются от этого Singleton сервиса.

- [x] таймаут на выполнение операции `TimeoutBusySchedule` (по умолчинию 5 секунд). Можно пере-назначить в наследуемых сервисах или во время выполнения программы через внедрённый Singleton сервис. Означает что сервис не может быть занят одним статусом долше чем установлено
- [x] сервис ведёт подробный лог работы в `ConcurrentBag<TracertItemModel> TracertChangeStatus`. Регистрируется каждая смена статуса методом `SetStatus()` (время изменения, и типа статуса: обычный статус или регистрациия ошибки). Размер и срок хранения трассировки определяются в параметрах `MaximumSizeSchedulerStatusTraceStack` (по умолчанию хранится не более 100 последнх строк/статусов) и `MaximumLifetimeSchedulerStatusTrace` (по умолчанию хранится не более 3 часов)
- [x] в реализуемых/наследуемых сервисах следует активно использовать `SetStatus()` для [ведения хронологии выполнения действий асинхронного потока](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/LocalbitcoinsBtcRateSingletonAsyncScheduler#demo-пример-визуализации-работы-сервиса-по-расписанию-1-раз-в-минуту-обновлять-курс). Доступ  к этим трассировкам далее возможен в любом месте. Простйшая визуализация в вашем .cshtml через внедрённый сервис. По окончанию выполнения асинхронного блока операций следует принудительно установить `SetStatus(null)`. Это сигнализирует о том что планировщик корректно выполнил задание и ожидает следуещего вызова. Если не освободить сервис принудительно то он освободиться (установится как null) по истечению `TimeoutBusySchedule` секунд. В трассировке этот инцидент будет зарегистрирован как ошибка
- [x] вспомогательные поля `SchedulerIsReady`, `LastChangeStatusDateTime` и `StartChangeStatusDateTime`
- [x] настройка в секундах паузы между повторами асинхронных операций `SchedulePausePeriod`. Наследуемый сервис обязан определить свой период, но его можно менять и во время работы программы через внедрённый Singleton сервис планировщика
- [x] активное поле `ScheduleStatus`. Хранит текущее состояние планировщика задач, но если состояние не null и зависло более чем на `TimeoutBusySchedule`, то автоматически сбрасывается на null. Статус null в свою очередь сообщает через `SchedulerIsReady` что сервис свободен.  В трассировке этот инцидент будет зарегистрирован как ошибка
- [x] в наследуемых сервисах асинхронный функционал описывается в определении абстрактоного метода `void AsyncScheduleAction()`. Базовый сервис оборачивает код этого метода в `await Task.Run(()=>{})`. см:`AsyncOperation()`