## Singleton сервисы асинхронных плановых задач

Базовый абстрактный сервис `AbstractAsyncScheduler` и реализации в наследуемых сервисах:

- [x] LocalbitcoinsBtcRateSingletonAsyncScheduler: _async_ _singleton_ сервис опроса публичного api от LocalBitcoins для определения актуального аргументированого (по сумме чека и методу оплаты) курса BTC
- [ ] ElectrumSingletonAsyncSheduler: _async_ _singleton_ сервис взаимодействия с Electrum JSONRPC
- [ ] TelegramBotSingletonAsyncSheduler: _async_ _singleton_ сервис подключения сервиса TelegramBot