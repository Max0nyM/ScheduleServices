# ScheduleServices
Семейство сервисов для подключения внешних WEB служб в проекты: ASP.Net Core 2.2

Набор (Scoped и Singleton) сервисов для интеграции со сторонними службами: LocalBitcoins, Electrum JSON/RPC и TelegramBot

Singleton сервисы асинхронно опрашивают сервисы (LocalBitcoins, Electrum JSON/RPC и TelegramBot) в [транзитные хранилища], а Scoped сервисы пишут данные из этих [хранилищ] в EF.Core базу.

## LocalBitcoins [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/LocalbitcoinsBtcRateSingletonAsyncScheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/LocalbitcoinsBtcRateScopedSyncScheduler)
driver beta version. readme in progress

## TelegramBot [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/TelegramBotSingletonAsyncSheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/TelegramBotScopedSyncScheduler)
driver test version ...

## Electrum JSON/RPC  [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/ElectrumSingletonAsyncSheduler)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped/ElectrumScopedSyncScheduler)
driver test version ...