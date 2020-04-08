using AbstractAsyncScheduler;
using CoinGeckoApi;
using MetadataEntityModel.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace CoinGeckoApiSingletonAsyncScheduler
{
    public class CoinGeckoApiSingletonAsyncScheduleService : BasicSingletonScheduler
    {
        readonly CoinGeckoClient client = new CoinGeckoClient();

        /// <summary>
        /// Текущий курс BTC (по состоянию последнего обновления)
        /// </summary>
        public double CurrentBtcRate { get; private set; }

        public ConcurrentBag<CoinGeckoSimplePriceModel> RatesBTC { get; set; } = new ConcurrentBag<CoinGeckoSimplePriceModel>();

        public CoinGeckoApiSingletonAsyncScheduleService(ILoggerFactory set_logger_factory, int set_schedule_pause_period)
            : base(set_logger_factory, set_schedule_pause_period)
        {
            ScheduleBodyAsyncAction();
        }

        protected override void ScheduleBodyAsyncAction()
        {
            SetStatus("Запрос к API-CoinGecko (не-авторизованый)");

            CoinGeckoSimplePriceModel answer = client.getPrice().Result;

            if (answer is null)
            {
                SetStatus("Ошибка получения данных с сервера API-CoinGecko", StatusTypes.ErrorStatus);

                SetStatus(null);
                return;
            }
            CurrentBtcRate = answer.bitcoin.rub;
            RatesBTC.Add(answer);
        }
    }
}
