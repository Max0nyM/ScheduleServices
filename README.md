# ScheduleServices
Семейство сервисов для подключения внешних WEB служб в проекты: ASP.Net Core 2.2

Набор (Scoped и Singleton) сервисов для интеграции со сторонними службами: LocalBitcoins, Electrum JSON/RPC и TelegramBot

Singleton сервисы асинхронно опрашивают сервисы (LocalBitcoins, Electrum JSON/RPC и TelegramBot) в [транзитные хранилища], а Scoped сервисы пишут данные из этих [хранилищ] в EF.Core базу.

## [LocalBitcoins](https://github.com/badhitman/ScheduleServices/tree/master/Singleton/LocalbitcoinsBtcRateSingletonAsyncScheduler) [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped)
driver is done. readme in progress

## TelegramBot
driver ed ...

## Electrum JSON/RPC
driver ed ...