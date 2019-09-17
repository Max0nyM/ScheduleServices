# ScheduleServices
Семейство сервисов для подключения внешних API служб в проекты: ASP.Net Core 2.2

Набор (Scoped и Singleton) сервисов для интеграции со сторонними службами: Electrum-JSON/RPC-API, LocalBitcoins-API и TelegramBot-API

Singleton сервисы асинхронно опрашивают API сервисы (Electrum JSON/RPC, LocalBitcoins и TelegramBot) во внутренние [транзитные хранилища], а Scoped сервисы пишут данные из этих [хранилищ] в EF.Core базу.

> Интерграция Singleton сервисов не требуют дополнительных доведений проекта. Просто подключаются через DI

> Интерграция Scoped сервисов требуют подготовку базы данных и обёртки для сервисов что бы получить доступ к Scoped контексту. Эти сервисы пишут в БД, а значит потребуется определить необходимые типы/таблицы в конечном проекте.

## LocalBitcoins [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/LocalbitcoinsBtcRateSingletonAsyncScheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/LocalbitcoinsBtcRateScopedSyncScheduler)
**beta** version.

## TelegramBot [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/TelegramBotSingletonAsyncSheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/TelegramBotScopedSyncScheduler)
**beta** version.

## Electrum JSON/RPC  [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/ElectrumSingletonAsyncSheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/ElectrumScopedSyncScheduler)
**beta** version.