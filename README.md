# ScheduleServices
Семейство сервисов для подключения служб в решения: ASP.Net Core 2.2

Набор (Scoped и Singleton) сервисов для интеграции в сторонние решения LocalBitcoins, Electrum JSON/RPC и TelegramBot служб.

Singleton сервисы асинхронно опрашивают сервисы (LocalBitcoins, Electrum JSON/RPC и TelegramBot) в [транзитные хранилища], а Scoped сервисы пишут данные из этих [хранилищ] в EF.Core базу.

## LocalBitcoins [Singleton](https://github.com/badhitman/ScheduleServices/tree/master/Singleton)+[Scoped](https://github.com/badhitman/ScheduleServices/tree/master/Scoped)

## Electrum JSON/RPC ...

## LocalBitcoins TelegramBot ...