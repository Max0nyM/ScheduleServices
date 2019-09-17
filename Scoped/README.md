## Scoped сервисы-адапторы транзита данных из [Async Singleton Service] в БД

Scoped сервисы-адапторы необходимы для извлечения транзитных данных из асинхронных сервисов и записи этих данных в BD

Базовый абстрактный `AbstractSyncScheduler` и реализации от него

- [x] **LocalbitcoinsBtcRateScopedSyncScheduler** _sync_ _scoped_ сервис-адаптор
- [x] **ElectrumScopedSyncScheduler** _sync_ _scoped_ сервис-адаптор
- [x] **TelegramBotScopedSyncScheduler** _sync_ _scoped_ сервис-адаптор