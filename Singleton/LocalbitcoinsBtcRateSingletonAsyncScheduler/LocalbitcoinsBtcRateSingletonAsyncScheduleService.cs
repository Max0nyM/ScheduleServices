////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using AbstractAsyncScheduler;
using LocalBitcoinsAPI;
using LocalBitcoinsAPI.Classes.lb_Serialize;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LocalbitcoinsBtcRateSingletonAsyncScheduler
{
    public class LocalbitcoinsBtcRateSingletonAsyncScheduleService : BasicSingletonScheduler
    {
        /// <summary>
        /// Транзитный набор полученых курсов с LocalBitcoins
        /// </summary>
        public ConcurrentBag<BtcRateLocalbitcoinsModel> RatesBTC { get; set; } = new ConcurrentBag<BtcRateLocalbitcoinsModel>();
        LocalBitcoins_API lb_api;

        public override bool SchedulerIsReady => AllowedPaymentMethods != null && base.SchedulerIsReady;

        /// <summary>
        /// Ограничение для отбора предложений биржи по минимальной сумме сделки.
        /// Значение должно быть разумным на столько что бы из текущих предложений на локале можно было легко найти подходящее.
        /// Исходя из этого ограничения приложение будет искать подходящие предложения именно под эту сумму
        /// </summary>
        public int SumFilter { get; private set; } = 5000;

        /// <summary>
        /// Ограничение для отбора предложений биржи по минимальной репутации трейдера.
        /// Значение должно быть разумным на столько что бы из текущих предложений на локале можно было легко найти подходящее.
        /// Исходя из этого ограничения приложение будет искать подходящие предложения с необходимым минимальным рейтингом трейдера.
        /// </summary>
        public int FilterProfileFeedbackScore { get; private set; } = 100;

        /// <summary>
        /// Текущий курс BTC (по состоянию последнего обновления)
        /// </summary>
        public double CurrentBtcRate { get; private set; }

        /// <summary>
        /// Код метода оплаты
        /// </summary>
        public string PaymentMethod { get; set; } = "qiwi";

        public string CurrentFiatCurrency { get; set; } = "rub";

        /// <summary>
        /// Доступные/Актуальные методы оплаты
        /// </summary>
        public Dictionary<string, string> AllowedPaymentMethods = new Dictionary<string, string>();

        public LocalbitcoinsBtcRateSingletonAsyncScheduleService(ILoggerFactory set_logger_factory, int set_schedule_pause_period, string set_local_bitcoins_api_auth_key, string set_auth_secret)
            : base(set_logger_factory, set_schedule_pause_period)
        {
            
            lb_api = new LocalBitcoins_API(set_local_bitcoins_api_auth_key, set_auth_secret);

            UpdatePaymentMethodsAsync();
        }

        private async void UpdatePaymentMethodsAsync()
        {
            await Task.Run(() =>
            {
                SetStatus("Загрузка доступных методов оплаты: " + GetType().Name);
                Dictionary<string, PaymentMethodsSerializationClass> raw_PaymentMethods = lb_api.PaymentMethods();
                if (raw_PaymentMethods is null)
                {
                    SetStatus("Ошибка загрузки методов оплаты. lb_api.PaymentMethods() вернул NULL", StatusTypes.ErrorStatus);
                    SetStatus(null);
                    return;
                }
                if (raw_PaymentMethods.Count() > 0)
                    SetStatus("lb_api.PaymentMethods() вернул [" + raw_PaymentMethods.Count() + "] объектов");
                else
                {
                    SetStatus("Ошибка! lb_api.PaymentMethods() вернул [0] объектов", StatusTypes.ErrorStatus);
                    SetStatus(null);
                    return;
                }
                raw_PaymentMethods = raw_PaymentMethods.Where(x => x.Value.currencies.Any(y => y.ToLower() == "rub")).ToDictionary(x => x.Key, t => t.Value);

                if (raw_PaymentMethods.Count() == 0)
                {
                    SetStatus("Ошибка! После отбора методов по валюте RUB, осталось [0] объектов", StatusTypes.ErrorStatus);
                    SetStatus(null);
                    return;
                }
                SetStatus("После отбора методов по валюте RUB, осталось [" + raw_PaymentMethods.Count() + "] объектов");

                AllowedPaymentMethods = raw_PaymentMethods.ToDictionary(x => x.Value.code, y => y.Value.name);
                ScheduleBodyAsyncAction();
            });
        }

        /// <summary>
        /// [async] запросить обновление BTC курса по публичным данным биржи LocalBitcoin
        /// </summary>
        protected override void ScheduleBodyAsyncAction()
        {
            SetStatus("Запрос к API-LocalBitcoins (не-авторизованый)");

            AdListBitcoinsOnlineSerializationClass adListBitcoins = lb_api.BuyBitcoinsOnline(null, null, CurrentFiatCurrency, PaymentMethod);
            if (adListBitcoins == null)
            {
                SetStatus("Ошибка получения данных с сервера API", StatusTypes.ErrorStatus);

                SetStatus("response body:" + lb_api.api_raw.responsebody, StatusTypes.ErrorStatus);

                SetStatus("http request status:" + lb_api.api_raw.HttpRequestStatus, StatusTypes.ErrorStatus);

                SetStatus(null);
                return;
            }

            lock (RatesBTC)
            {
                List<AdListItem> buy_bitcoin_online = adListBitcoins.data.ad_list.Where(x => x.data.get_min_amount_as_double() <= SumFilter && x.data.profile.feedback_score >= FilterProfileFeedbackScore && x.data.get_temp_price() > 0) as List<AdListItem>;
                if (buy_bitcoin_online is null)
                    buy_bitcoin_online = new List<AdListItem>();

                string pagination_next = adListBitcoins.pagination?.next;
                while (adListBitcoins is null || (buy_bitcoin_online.Count() < 5 && !string.IsNullOrWhiteSpace(pagination_next)))
                {
                    adListBitcoins = lb_api.BuyBitcoinsOnline(null, null, null, null, pagination_next);
                    if (adListBitcoins is null)
                    {
                        SetStatus("Ошибка получения данных от сервера", StatusTypes.ErrorStatus);

                        string HttpRequestStatus = lb_api.api_raw.HttpRequestStatus;

                        SetStatus("HttpRequestStatus: " + HttpRequestStatus, StatusTypes.ErrorStatus);

                        string responsebody = lb_api.api_raw.responsebody;

                        SetStatus("ResponseBody: " + responsebody, StatusTypes.ErrorStatus);

                        SetStatus("Ещё одна попытка...");

                        continue;
                    }

                    SetStatus("Получено [" + adListBitcoins.data.ad_list.Count() + "] объявлений");

                    pagination_next = adListBitcoins.pagination?.next;
                    IEnumerable<AdListItem> ie_ads = adListBitcoins.data.ad_list.Where(x => x.data.get_min_amount_as_double() <= SumFilter && x.data.profile.feedback_score >= FilterProfileFeedbackScore && x.data.get_temp_price() > 0);
                    if (ie_ads != null)
                    {
                        SetStatus("После применения фильтров к объявлениям осталось [" + ie_ads.Count() + "] объявлений");

                        buy_bitcoin_online.AddRange(ie_ads.ToList());
                    }
                }

                if (buy_bitcoin_online.Count() == 0)
                {
                    SetStatus("Ошибка получения курса. Ни одного предложения.", StatusTypes.ErrorStatus);
                    SetStatus(null);
                    return;
                }

                BtcRateLocalbitcoinsModel btcRate = new BtcRateLocalbitcoinsModel() { CountRates = buy_bitcoin_online.Count, DateCreate = DateTime.Now, Information = "Снимок состяния Localbitcoins qiwi", MaxRate = double.MinValue, MinRate = double.MaxValue };
                foreach (AdListItem ad_item in buy_bitcoin_online.Take(4))
                {
                    btcRate.MaxRate = Math.Max(btcRate.MaxRate, ad_item.data.get_temp_price());
                    btcRate.MinRate = Math.Min(btcRate.MinRate, ad_item.data.get_temp_price());
                }

                RatesBTC.Add(btcRate);
                RatesBTC = new ConcurrentBag<BtcRateLocalbitcoinsModel>(RatesBTC.OrderBy(x => x.DateCreate));
                if (MaxSizeTransit < RatesBTC.Count)
                    RatesBTC = new ConcurrentBag<BtcRateLocalbitcoinsModel>(RatesBTC.Skip(RatesBTC.Count - MaxSizeTransit));
                // public ConcurrentBag<BtcRateLocalbitcoinsModel> RatesBTC
                SetStatus(null);

                SetStatus("В памяти зафиксирована информация Localbitcoins RatesBTC[" + RatesBTC.Count + "]");

                CurrentBtcRate = (btcRate.MaxRate + btcRate.MinRate) / 2;
                SetStatus(null);
            }
        }

        public override void InvokeSchedule()
        {
            if (AllowedPaymentMethods is null || AllowedPaymentMethods.Count() == 0)
            {
                SetStatus("Методы оплаты не загружены. Вызов планировщика невозможен", StatusTypes.ErrorStatus);
                if(string.IsNullOrWhiteSpace(ScheduleStatus))
                {
                    SetStatus("Повторная попытка загрузка методов оплаты");
                    UpdatePaymentMethodsAsync();
                }
                else
                    SetStatus(null);

                return;
            }

            if (!AllowedPaymentMethods.Any(x => x.Key.ToLower() == PaymentMethod.ToLower()))
            {
                SetStatus("Запрошеный метод оплаты не найден", StatusTypes.ErrorStatus);
                SetStatus(null);
                return;
            }

            base.InvokeSchedule();
        }
    }
}
