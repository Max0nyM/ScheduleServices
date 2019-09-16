////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using AbstractSyncScheduler;
using MetadataEntityModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TelegramBot.TelegramMetadata.GettingUpdates;
using TelegramBotSingletonAsyncSheduler;

namespace TelegramBotScopedSyncScheduler
{
    public class TelegramBotScopedSyncScheduleService : BasicScopedSyncScheduler
    {
        public enum ModesAutoAnswerTelegramBot
        {
            /// <summary>
            /// Один ответ на все запросы. Бот отвечает всем одно и то же из AloneAnswerMessage
            /// </summary>
            AloneAnswer,

            /// <summary>
            /// Ответ ВСЕМ в автоматическом режиме. Режим ответа определяется из параметров самого пользователя
            /// </summary>
            AllAnswer,

            /// <summary>
            /// Ответ ТОЛЬКО РАЗРЕШЁННЫМ (из списка AllowedTelegramUsers) в автоматическом режиме
            /// </summary>
            AllovedOnly
        }

        /// <summary>
        /// Режим TelegramBot ответа на запросы.
        /// Один ответ на все запросы AloneAnswerMessage
        /// </summary>
        public ModesAutoAnswerTelegramBot ModeAutoAnswerTelegramBot { get; set; } = ModesAutoAnswerTelegramBot.AllAnswer;

        public List<long> AllowedTelegramUsers = new List<long>();

        public virtual string AloneAnswerMessage { get; } = "TelegramBot в данный момент обслуживается техническими специалистами";
        public TelegramBotSingletonAsyncScheduleService AsyncTelegramBotScheduleService => (TelegramBotSingletonAsyncScheduleService)BasicSingletonService;
        protected List<TelegramBotRequestModel> TelegramBotRequestStack = new List<TelegramBotRequestModel>();
        public TelegramBotScopedSyncScheduleService(DbContext set_db, TelegramBotSingletonAsyncScheduleService set_async_telegram_bot_schedule_service)
            : base(set_db, set_async_telegram_bot_schedule_service)
        {
            //AsyncTelegramBotScheduleService = set_async_telegram_bot_schedule_service;

        }

        public override void SyncUpdate()
        {
            AsyncTelegramBotScheduleService.SetStatus("Запуск синхронной части планировщика", AbstractAsyncScheduler.StatusTypes.DebugStatus);
            if (string.IsNullOrWhiteSpace(AsyncTelegramBotScheduleService.TelegramBotApiKey))
            {
                AsyncTelegramBotScheduleService.SetStatus("Ошибка запуска автоответчика. Не установлен TelegramBotApiKey ", AbstractAsyncScheduler.StatusTypes.ErrorStatus);
                AsyncTelegramBotScheduleService.SetStatus(null);
                return;
            }

            if (AsyncTelegramBotScheduleService?.TelegramClient?.Me is null && string.IsNullOrWhiteSpace(AsyncTelegramBotScheduleService?.ScheduleStatus))
            {
                AsyncTelegramBotScheduleService.SetStatus("Авторизация бота");
                AsyncTelegramBotScheduleService.AuthTelegramBotAsync();
                return;
            }

            lock (AsyncTelegramBotScheduleService.TelegramBotUpdates)
            {
                lock (AsyncTelegramBotScheduleService.TelegramBotUpdates)
                {
                    AsyncTelegramBotScheduleService.SetStatus("Чтение транзитного хранилища [" + AsyncTelegramBotScheduleService.TelegramBotUpdates.Count + "] строк", (AsyncTelegramBotScheduleService.TelegramBotUpdates.Count > 0 ? AbstractAsyncScheduler.StatusTypes.SetValueStatus : AbstractAsyncScheduler.StatusTypes.DebugStatus));
                    foreach (IGrouping<long, Update> group_updates_by_sender in AsyncTelegramBotScheduleService.TelegramBotUpdates.GroupBy(x => x.message.from.id).ToList())
                    {
                        AsyncTelegramBotScheduleService.SetStatus("UserModel telegram_user = db.Users.SingleOrDefault(x => x.TelegramId == " + group_updates_by_sender.Key + ");");
                        UserModel telegram_user = db.Set<UserModel>().SingleOrDefault(x => x.TelegramId == group_updates_by_sender.Key);

                        if (telegram_user == null)
                        {
                            telegram_user = new UserModel()
                            {
                                AccessLevel = AccessLevelUserModel.Auth,
                                Information = "telegram auto init user",
                                TelegramId = group_updates_by_sender.Key,
                                AboutTelegramUser = group_updates_by_sender.First().message.from.ToString()
                            };

                            db.Add(telegram_user);
                            db.SaveChanges();
                        }
                        else
                        {
                            telegram_user.LastTelegramActiv = DateTime.Now;
                            telegram_user.AboutTelegramUser = group_updates_by_sender.First().message.from.ToString();
                            db.Update(telegram_user);
                            db.SaveChanges();
                        }
                        foreach (Update u in group_updates_by_sender.ToList())
                        {
                            TelegramUpdateModel telegramUpdate = new TelegramUpdateModel() { Information = u.GetAsString(), UserId = telegram_user.Id };

                            db.Add(telegramUpdate);
                            db.SaveChanges();
                        }

                        if (ModeAutoAnswerTelegramBot == ModesAutoAnswerTelegramBot.AloneAnswer || (ModeAutoAnswerTelegramBot == ModesAutoAnswerTelegramBot.AllovedOnly && !AllowedTelegramUsers.Contains(telegram_user.TelegramId)))
                            AsyncTelegramBotScheduleService.SendMessageTelegramBotAsync(telegram_user.TelegramId, AloneAnswerMessage);
                        else
                            TelegramBotRequestStack.Add(new TelegramBotRequestModel() { User = telegram_user, Updates = group_updates_by_sender.ToList() });
                    }
                    AsyncTelegramBotScheduleService.TelegramBotUpdates.Clear();
                }
            }
            AsyncTelegramBotScheduleService.SetStatus(null);
        }
    }
}
