using AbstractAsyncScheduler;
using AbstractSyncScheduler;
using CoinGeckoApiSingletonAsyncScheduler;
using LocalbitcoinsBtcRateSingletonAsyncScheduler;
using MetadataEntityModel.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CoinGeckoApiScopedSyncScheduler
{
    public class CoinGeckoApiScopedSyncScheduleService : BasicScopedSyncScheduler
    {
        public CoinGeckoApiSingletonAsyncScheduleService AsyncCoinGeckoBtcRateScheduleService => (CoinGeckoApiSingletonAsyncScheduleService)BasicSingletonService;

        public CoinGeckoApiScopedSyncScheduleService(DbContext set_db, CoinGeckoApiSingletonAsyncScheduleService set_async_coin_gecko_api_schedule_service)
            : base(set_db, set_async_coin_gecko_api_schedule_service)
        {
            if (IsReady && AsyncCoinGeckoBtcRateScheduleService.RatesBTC.Count > 0)
            {
                BasicSingletonService.SetStatus("Запуск sync scoped service", StatusTypes.DebugStatus);
                SyncUpdate();
                BasicSingletonService.SetStatus(null, StatusTypes.DebugStatus);
            }
        }

        public override void SyncUpdate()
        {
            AsyncCoinGeckoBtcRateScheduleService.SetStatus("Вызов scoped сервиса для записи данных в БД");
            lock (AsyncCoinGeckoBtcRateScheduleService.RatesBTC)
            {
                foreach (CoinGeckoSimplePriceModel btcRate in AsyncCoinGeckoBtcRateScheduleService.RatesBTC.OrderBy(x => x.time))
                {
                    BtcRateLocalbitcoinsModel btcRateObj = new BtcRateLocalbitcoinsModel()
                    {
                        CountRates = 1,
                        MaxRate = btcRate.bitcoin.rub,
                        MinRate = btcRate.bitcoin.rub,
                        Information = "load from CoinGecko"
                    };

                    db.Add(btcRateObj);
                    db.SaveChanges();
                    AsyncCoinGeckoBtcRateScheduleService.SetStatus("Загружается снимок состояния: " + btcRate.ToString());
                }
                AsyncCoinGeckoBtcRateScheduleService.RatesBTC.Clear();
            }

            AsyncCoinGeckoBtcRateScheduleService.SetStatus(null);
        }
    }
}
