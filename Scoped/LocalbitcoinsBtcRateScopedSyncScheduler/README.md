## Scoped LocalBitcoins BTC rates Service

Реализует перенос данных из транзитного хранилища в Async Singleton Service в базу данных.

Для внедрения контекста базы данных (в данном контексте _Scoped_ _Service_ **AppDbContext set_db**) вашего приложения - вопервых данный сервис-тип прийдётся обернуть в:

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

Во вторых обратите внимание, что для иньекции зависимости от LocalBitcoins Singleton Service пришлось обернуть в:
```C#
public class MyLocalbitcoinsBtcRateSingletonAsyncScheduleService : LocalbitcoinsBtcRateSingletonAsyncScheduleService
{
	public MyLocalbitcoinsBtcRateSingletonAsyncScheduleService(IOptions<AppConfig> options, ILoggerFactory set_logger_factory)
		: base(set_logger_factory, options.Value.LocalBitcoinsApiSchedulePauseDuration, options.Value.LocalBitcoinsApiAuthKey, options.Value.LocalBitcoinsApiAuthSecret)
	{
		
	}
}
```

Пример добавления сервисов:
```C#
public static class ServiceProviderExtensions
{
	public static void AddLocalbitcoinsBtcRatesService(this IServiceCollection services)
	{
		services.AddSingleton<MyLocalbitcoinsBtcRateSingletonAsyncScheduleService>();
		services.AddScoped<MyLocalbitcoinsBtcRateScopedSyncScheduleService>();
	}
}
```

И последнее: теперь необходимо обеспечить регулярный вызов какого либо middleware с внедрённым в него нашего LocalBitcoins Scoped Service, что бы транзит данных не заставивался.

Предлагается обеспечить пинг сервера с паузой например в 1 сикунду - чем гарантировать регулярность обработки транзитных данных из Singleton в Scoped и так в базу данных

> **P.S.** Если данный Scoped Sync Service не нужен, то Singleton Async Service может внедряться без обёртки