## Scoped LocalBitcoins BTC rates Service

Реализует перенос данных из транзитного хранилища в Async Singleton Service в базу данных.

Для внедрения контекста scoped сервиса базы данных вашего приложения - вопервых данный тип следует обернуть в свойм проекте

```C#
public class MyLocalbitcoinsBtcRateScopedSyncScheduleService : LocalbitcoinsBtcRateScopedSyncScheduleService
{
	public MyLocalbitcoinsBtcRateScopedSyncScheduleService(AppDbContext set_db, MyLocalbitcoinsBtcRateSingletonAsyncScheduleService set_localbitcoins_btc_rate_singleton_async_schedule_service)
		: base(set_db, set_localbitcoins_btc_rate_singleton_async_schedule_service)
	{
		
	}
}
```
Это даст доступ к контексту вашей базы данных, но это ещё не всё

Во вторых для иньекции зависимости от LocalBitcoins Singleton Service прийдётся обернуть в:
```C#
public class MyLocalbitcoinsBtcRateSingletonAsyncScheduleService : LocalbitcoinsBtcRateSingletonAsyncScheduleService
{
	public MyLocalbitcoinsBtcRateSingletonAsyncScheduleService(IOptions<AppConfig> options, ILoggerFactory set_logger_factory)
		: base(set_logger_factory, options.Value.LocalBitcoinsApiSchedulePauseDuration, options.Value.LocalBitcoinsApiAuthKey, options.Value.LocalBitcoinsApiAuthSecret)
	{
		
	}
}
```