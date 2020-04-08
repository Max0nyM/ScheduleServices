using AbstractAsyncScheduler;
using AbstractSyncScheduler;
using BitcoinAverageApiSingletonAsyncScheduler;
using LocalbitcoinsBtcRateSingletonAsyncScheduler;
using MetadataEntityModel.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CoinGeckoApiScopedSyncScheduler
{
    public class BitcoinAverageApiScopedSyncScheduleService : BasicScopedSyncScheduler
    {
        public BitcoinAverageApiSingletonAsyncScheduleService AsyncBitcoinAverageBtcRateScheduleService => (BitcoinAverageApiSingletonAsyncScheduleService)BasicSingletonService;

        public BitcoinAverageApiScopedSyncScheduleService(DbContext set_db, BitcoinAverageApiSingletonAsyncScheduleService set_async_bitcoin_average_api_schedule_service)
            : base(set_db, set_async_bitcoin_average_api_schedule_service)
        {
            if (IsReady && AsyncBitcoinAverageBtcRateScheduleService.RatesBTC.Count > 0)
            {
                BasicSingletonService.SetStatus("Запуск sync scoped service", StatusTypes.DebugStatus);
                SyncUpdate();
                BasicSingletonService.SetStatus(null, StatusTypes.DebugStatus);
            }
        }

        public override void SyncUpdate()
        {
            AsyncBitcoinAverageBtcRateScheduleService.SetStatus("Вызов scoped сервиса для записи данных в БД");
            lock (AsyncBitcoinAverageBtcRateScheduleService.RatesBTC)
            {
                foreach (BitcoinAverageConvertModel btcRate in AsyncBitcoinAverageBtcRateScheduleService.RatesBTC.OrderBy(x => DateTime.Parse(x.time)))
                {
                    double CurrentBtcRate = Math.Round(1 / btcRate.price * (double)AsyncBitcoinAverageBtcRateScheduleService.SumFilter, 2);
                   BtcRateLocalbitcoinsModel btcRateObj = new BtcRateLocalbitcoinsModel()
                    {
                        CountRates = 1,
                        MaxRate = CurrentBtcRate,
                        MinRate = CurrentBtcRate,
                        Information = "load from BitcoinAverageConvert [success: " + btcRate.success + "] [time:" + btcRate.time + "]"
                    };

                    db.Add(btcRateObj);
                    db.SaveChanges();
                    AsyncBitcoinAverageBtcRateScheduleService.SetStatus("Загружается снимок состояния: " + btcRate.ToString());
                }
                AsyncBitcoinAverageBtcRateScheduleService.RatesBTC.Clear();
            }
            
            AsyncBitcoinAverageBtcRateScheduleService.SetStatus(null);
        }
    }
}
