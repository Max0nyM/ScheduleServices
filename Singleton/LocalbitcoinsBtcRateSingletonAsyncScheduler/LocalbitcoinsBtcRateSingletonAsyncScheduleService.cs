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

        public override bool SchedulerIsReady => PaymentMethods != null && base.SchedulerIsReady;

        /// <summary>
        /// Ограничение для отбора предложений биржи по минимальной сумме сделки.
        /// Значение должно быть разумным на столько что бы из текущих предложений на локале можно было легко найти подходящее.
        /// Исходя из этого ограничения приложение будет искать подходящие предложения именно под эту сумму
        /// </summary>
        public int SumFilter { get; private set; } = 1500;

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
        public string PaymentMethod { get; private set; }

        /// <summary>
        /// Доступные/Актуальные методы оплаты
        /// </summary>
        public Dictionary<string, string> PaymentMethods = new Dictionary<string, string>();

        public LocalbitcoinsBtcRateSingletonAsyncScheduleService(ILoggerFactory loggerFactory, string set_payment_method = "qiwi") : base(loggerFactory)
        {
            /// <summary>
            /// Пауза в секундах между обновлениями данных с сервера
            /// </summary>
            SchedulePausePeriod = 60 * 30;

            PaymentMethod = set_payment_method.ToLower();
            string msg_text = "Инициализация " + GetType().Name;
            SetStatus(msg_text);
            AppLogger.LogInformation(msg_text);
            lb_api = new LocalBitcoins_API("auth key", "auth secret");
            
            AppLogger.LogInformation(msg_text);
            UpdatePaymentMethodsAsync();
        }

        private async void UpdatePaymentMethodsAsync()
        {
            await Task.Run(() =>
            {
                string msg_text = "Загрузка доступных методов оплаты: " + GetType().Name;
                AppLogger.LogInformation(msg_text);
                SetStatus(msg_text);
                Dictionary<string, PaymentMethodsSerializationClass> raw_PaymentMethods = lb_api.PaymentMethods();
                if (raw_PaymentMethods is null)
                {
                    msg_text = "Ошибка загрузки методов оплаты. lb_api.PaymentMethods() вернул NULL";
                    SetStatus(msg_text, StatusTypes.ErrorStatus);
                    AppLogger.LogError(msg_text);
                    SetStatus(null);
                    return;
                }
                if (raw_PaymentMethods.Count() > 0)
                {
                    msg_text = "lb_api.PaymentMethods() вернул [" + raw_PaymentMethods.Count() + "] объектов";
                    SetStatus(msg_text);
                    AppLogger.LogInformation(msg_text);
                }
                else
                {
                    msg_text = "Ошибка! lb_api.PaymentMethods() вернул [0] объектов";
                    SetStatus(msg_text, StatusTypes.ErrorStatus);
                    AppLogger.LogError(msg_text);
                    SetStatus(null);
                    return;
                }
                raw_PaymentMethods = raw_PaymentMethods.Where(x => x.Value.currencies.Any(y => y.ToLower() == "rub")).ToDictionary(x => x.Key, t => t.Value);

                if (raw_PaymentMethods.Count() == 0)
                {
                    msg_text = "Ошибка! После отбора методов по валюте RUB, осталось [0] объектов";
                    SetStatus(msg_text, StatusTypes.ErrorStatus);
                    AppLogger.LogError(msg_text);
                    SetStatus(null);
                    return;
                }
                msg_text = "После отбора методов по валюте RUB, осталось [" + raw_PaymentMethods.Count() + "] объектов";
                SetStatus(msg_text);
                AppLogger.LogInformation(msg_text);
                PaymentMethods = raw_PaymentMethods.ToDictionary(x => x.Value.code, y => y.Value.name);
                AsyncScheduleAction();
            });
        }

        /// <summary>
        /// [async] запросить обновление BTC курса по публичным данным биржи LocalBitcoin
        /// </summary>
        protected override void AsyncScheduleAction()
        {
            string msg_text = "Запрос к API-LocalBitcoins (не-авторизованый)";
            SetStatus(msg_text);
            AppLogger.LogDebug(msg_text);

            AdListBitcoinsOnlineSerializationClass adListBitcoins = lb_api.BuyBitcoinsOnline(null, null, "rub", PaymentMethod);
            if (adListBitcoins == null)
            {
                msg_text = "Ошибка получения данных с сервера API";
                SetStatus(msg_text, StatusTypes.ErrorStatus);
                AppLogger.LogError(msg_text);

                msg_text = "response body:" + lb_api.api_raw.responsebody;
                SetStatus( msg_text, StatusTypes.ErrorStatus);
                AppLogger.LogError(msg_text);

                msg_text = "http request status:" + lb_api.api_raw.HttpRequestStatus;
                SetStatus(msg_text, StatusTypes.ErrorStatus);
                AppLogger.LogError(msg_text);

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
                        msg_text = "Ошибка получения данных от сервера";
                        SetStatus(msg_text, StatusTypes.ErrorStatus);
                        AppLogger.LogError(msg_text);

                        string HttpRequestStatus = lb_api.api_raw.HttpRequestStatus;
                        msg_text = "HttpRequestStatus: " + HttpRequestStatus;
                        SetStatus(msg_text, StatusTypes.ErrorStatus);
                        AppLogger.LogError(msg_text);

                        string responsebody = lb_api.api_raw.responsebody;
                        msg_text = "ResponseBody: " + responsebody;
                        SetStatus(msg_text, StatusTypes.ErrorStatus);
                        AppLogger.LogError(msg_text);

                        msg_text = "Ещё одна попытка...";
                        SetStatus(msg_text);
                        AppLogger.LogTrace(msg_text);

                        continue;
                    }

                    msg_text = "Получено [" + adListBitcoins.data.ad_list.Count() + "] объявлений";
                    SetStatus(msg_text);
                    AppLogger.LogInformation(msg_text);

                    pagination_next = adListBitcoins.pagination?.next;
                    IEnumerable<AdListItem> ie_ads = adListBitcoins.data.ad_list.Where(x => x.data.get_min_amount_as_double() <= SumFilter && x.data.profile.feedback_score >= FilterProfileFeedbackScore && x.data.get_temp_price() > 0);
                    if (ie_ads != null)
                    {
                        msg_text = "После применения фильтров к объявлениям осталось [" + ie_ads.Count() + "] объявлений";
                        SetStatus(msg_text);
                        AppLogger.LogInformation(msg_text);

                        buy_bitcoin_online.AddRange(ie_ads.ToList());
                    }
                }

                if (buy_bitcoin_online.Count() == 0)
                {
                    msg_text = "Ошибка получения курса. Ни одного предложения.";
                    AppLogger.LogError(msg_text);
                    SetStatus(msg_text, StatusTypes.ErrorStatus);
                    SetStatus(null);
                    return;
                }

                BtcRateLocalbitcoinsModel btcRate = new BtcRateLocalbitcoinsModel() { CountRates = buy_bitcoin_online.Count, DateCreate = DateTime.Now, Information = "Снимок состяния Localbitcoins qiwi", MaxRate = double.MinValue, MinRate = double.MaxValue };
                foreach (AdListItem ad_item in buy_bitcoin_online.Take(4))
                {
                    btcRate.MaxRate = Math.Max(btcRate.MaxRate, ad_item.data.get_temp_price());
                    btcRate.MinRate = Math.Min(btcRate.MinRate, ad_item.data.get_temp_price());
                }
                msg_text = "Сформирован снимок состояния Localbitcoins AdList";
                AppLogger.LogWarning(msg_text);
                SetStatus(msg_text);

                RatesBTC.Add(btcRate);
                SetStatus(null);

                CurrentBtcRate = (btcRate.MaxRate + btcRate.MinRate) / 2;
            }
        }

        public new void InvokeAsyncSchedule()
        {
            if (PaymentMethods is null || PaymentMethods.Count() == 0)
            {
                SetStatus("Методы оплаты не загружены. Вызов планировщика отклоняется.", StatusTypes.ErrorStatus);
                SetStatus(null);
                return;
            }

            if (!PaymentMethods.Any(x => x.Key.ToLower() == PaymentMethod.ToLower()))
            {
                SetStatus("Запрошеный метод оплаты не найден", StatusTypes.ErrorStatus);
                SetStatus(null);
                return;
            }

            base.InvokeAsyncSchedule();
        }
    }
}
