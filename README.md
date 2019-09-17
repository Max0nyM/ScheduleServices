# ScheduleServices
Семейство сервисов для подключения внешних API служб в проекты: ASP.Net Core 2.2

Набор ([Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped) и [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton)) сервисов для интеграции со сторонними службами: [Electrum-JSON/RPC-API](https://github.com/spesmilo/electrum/blob/master/electrum/commands.py), [LocalBitcoins-API](https://localbitcoins.net/api-docs/) и [TelegramBot-API](https://core.telegram.org/bots/api)

Singleton сервисы асинхронно опрашивают API сервисы (Electrum JSON/RPC, LocalBitcoins и TelegramBot) во внутренние [транзитные хранилища], а Scoped сервисы пишут данные из этих [хранилищ] в EF.Core базу.

> Интерграция Singleton сервисов не требуют дополнительных доведений проекта. Просто подключаются через DI. Внутри сервисов будет идти ротация данных, доступ к которым открыт всем через доступ ConcurrentBag<T>.

> Интерграция Scoped сервисов требуют подготовку базы данных и обёртки для сервисов что бы получить доступ к Scoped контексту. Эти сервисы пишут в БД, а значит потребуется определить необходимые типы/таблицы в конечном проекте.

## LocalBitcoins [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/LocalbitcoinsBtcRateSingletonAsyncScheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/LocalbitcoinsBtcRateScopedSyncScheduler)
**beta** version.

## TelegramBot [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/TelegramBotSingletonAsyncSheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/TelegramBotScopedSyncScheduler)
**beta** version.

## Electrum JSON/RPC  [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/ElectrumSingletonAsyncSheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/ElectrumScopedSyncScheduler)
**beta** version.