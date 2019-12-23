using AbstractAsyncScheduler;
using BitcoinAverageApi;
using BitcoinAverageApi.Model;
using MetadataEntityModel.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace BitcoinAverageApiSingletonAsyncScheduler
{
    public class BitcoinAverageApiSingletonAsyncScheduleService : BasicSingletonScheduler
    {
        BitcoinAverageClient client = new BitcoinAverageClient();

        /// <summary>
        /// Текущий курс BTC (по состоянию последнего обновления)
        /// </summary>
        public double CurrentBtcRate { get; private set; }

        public int SumFilter { get; private set; }

        /// <summary>
        /// Транзитный набор полученых курсов с LocalBitcoins
        /// </summary>
        public ConcurrentBag<BitcoinAverageConvertModel> RatesBTC { get; set; } = new ConcurrentBag<BitcoinAverageConvertModel>();

        public BitcoinAverageApiSingletonAsyncScheduleService(ILoggerFactory set_logger_factory, int set_schedule_pause_period, int setSumFilter = 1000)
            : base(set_logger_factory, set_schedule_pause_period)
        {
            SumFilter = setSumFilter;
            ScheduleBodyAsyncAction();
        }

        protected override void ScheduleBodyAsyncAction()
        {
            SetStatus("Запрос к API-BitcoinAverage (не-авторизованый)");

            BitcoinAverageConvertModel answer = client.getConvert(SumFilter).Result;

            if (answer is null)
            {
                SetStatus("Ошибка получения данных с сервера API-BitcoinAverage", StatusTypes.ErrorStatus);

                SetStatus(null);
                return;
            }
            CurrentBtcRate = Math.Round(1 / answer.price * SumFilter,2);
            RatesBTC.Add(answer);
        }
    }
}
