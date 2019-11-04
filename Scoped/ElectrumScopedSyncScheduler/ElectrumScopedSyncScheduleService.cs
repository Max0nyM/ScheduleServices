////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using AbstractAsyncScheduler;
using AbstractSyncScheduler;
using ElectrumJSONRPC.Response.Model;
using ElectrumSingletonAsyncSheduler;
using MetadataEntityModel;
using Microsoft.EntityFrameworkCore;
using MultiTool;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ElectrumScopedSyncScheduler
{
    public class ElectrumScopedSyncScheduleService : BasicScopedSyncScheduler
    {
        static List<string> busy_addresses = new List<string>();
        public virtual int MinRequedCountConfirmations { get; set; } = 1;
        public ElectrumJsonRpcSingletonAsyncScheduleService AsyncElectrumScheduleService => BasicSingletonService as ElectrumJsonRpcSingletonAsyncScheduleService;
        public ElectrumScopedSyncScheduleService(DbContext set_db, ElectrumJsonRpcSingletonAsyncScheduleService set_async_electrum_schedule_service)
            : base(set_db, set_async_electrum_schedule_service)
        {
            if (IsReady && AsyncElectrumScheduleService.Transactions.Count > 0)
            {
                BasicSingletonService.SetStatus("Запуск sync scoped service", StatusTypes.DebugStatus);
                SyncUpdate();
                BasicSingletonService.SetStatus(null, StatusTypes.DebugStatus);
            }
        }

        public override void SyncUpdate()
        {
            lock (AsyncElectrumScheduleService.Transactions)
            {
                AsyncElectrumScheduleService.SetStatus("Сверка транзакций из Electrum [" + AsyncElectrumScheduleService.Transactions.Count + " элементов] с базой данных");
                bool exist_new_tx = false;
                foreach (TransactionWalletHistoryResponseClass TransactionWallet in AsyncElectrumScheduleService.Transactions.Where(x => x.confirmations > MinRequedCountConfirmations))
                {
                    if (string.IsNullOrWhiteSpace(TransactionWallet.txid))
                    {
                        AsyncElectrumScheduleService.SetStatus("Прочитана транзакция из Electrum JSONRPC с пустым txid: " + TransactionWallet.ToString(), AbstractAsyncScheduler.StatusTypes.ErrorStatus);
                        AsyncElectrumScheduleService.SetStatus("Транзакция с пустым txid будет пропущена");
                        continue;
                    }

                    BtcTransactionModel btcTransaction;
                    try
                    {
                        btcTransaction = db.Set<BtcTransactionModel>().SingleOrDefault(x => x.TxId == TransactionWallet.txid);
                    }
                    catch (Exception e)
                    {
                        AsyncElectrumScheduleService.SetStatus("Ошибка поиска транзакции в БД SingleOrDefault(x => x.TxId == '" + TransactionWallet.txid + "')" + e.Message, AbstractAsyncScheduler.StatusTypes.ErrorStatus);
                        AsyncElectrumScheduleService.SetStatus("Ошибочная транзакция будет пропущена");
                        continue;
                    }

                    if (btcTransaction is null)
                    {
                        exist_new_tx = true;
                        AsyncElectrumScheduleService.SetStatus("Новая транзакция для записи в БД: " + TransactionWallet.ToString());
                        btcTransaction = new BtcTransactionModel()
                        {
                            TxId = TransactionWallet.txid,
                            Sum = glob_tools.GetDoubleFromString(TransactionWallet.value)
                        };

                        db.Add(btcTransaction);
                        db.SaveChanges();

                        foreach (TransactionWalletHistoryResponseOutputsClass TransactionOut in TransactionWallet.outputs.Where(x => AsyncElectrumScheduleService.ElectrumClient?.IsAddressMine(x.address)?.result == true))
                        {
                            AsyncElectrumScheduleService.SetStatus("Запись нового TxOut: " + TransactionOut.ToString());
                            BtcTransactionOutModel btcTransactionOut = new BtcTransactionOutModel()
                            {
                                BtcTransactionModelId = btcTransaction.Id,
                                Sum = glob_tools.GetDoubleFromString(TransactionOut.value),
                                Information = "txid:" + TransactionWallet.txid,
                                Address = TransactionOut.address,
                                IsMine = AsyncElectrumScheduleService.ElectrumClient.IsAddressMine(TransactionOut.address)?.result ?? false
                            };
                            db.Add(btcTransactionOut);
                            db.SaveChanges();

                            AsyncElectrumScheduleService.SetStatus("Поиск пользователя по BTC адресу > db.Users.SingleOrDefault(x => x.BitcoinAddress == '" + TransactionOut.address + "')");
                            UserModel user = db.Set<UserModel>().SingleOrDefault(x => x.BitcoinAddress == TransactionOut.address);
                            if (!(user is null))
                            {
                                AsyncElectrumScheduleService.SetStatus("Пользователь найден: " + user.ToString());
                                btcTransactionOut.UserId = user.Id;
                                db.Update(btcTransactionOut);
                                //
                                ///int fiat_sum = (int)(btcTransactionOut.Sum * options.Value.CurrentBtcRate);
                                user.BalanceBTC += btcTransactionOut.Sum;
                                db.Update(user);

                                string notify = "Пополнение /balance +" + string.Format("{0:F8}", Math.Round(btcTransactionOut.Sum, 8)) + "=" + user.BalanceBTC + " BTC";

                                db.Add(new eCommerceJournalModel()
                                {
                                    BaseObjectId = btcTransactionOut.Id,
                                    TypeBaseObject = TypesBaseObject.TxOut,
                                    ClientId = user.Id,
                                    SumBTC = btcTransactionOut.Sum,
                                    Information = notify
                                });
                                db.Add(new MessageModel() { Information = notify, SenderId = null, RecipientId = user.Id, NeedTelegramNotify = user.TelegramId != default });

                                db.SaveChanges();

                                foreach (UserModel admin_user in db.Set<UserModel>().Where(x => x.AccessLevel > AccessLevelUserModel.Manager))
                                {
                                    db.Add(new MessageModel() { Information = "Для пользователя: ["+ user?.Id +"]" + user?.AboutTelegramUser + " >> " + notify, SenderId = user?.Id, RecipientId = admin_user.Id, NeedTelegramNotify = user.TelegramId != default });
                                }
                                db.SaveChanges();
                            }
                            else
                                AsyncElectrumScheduleService.SetStatus("Пользователь с таким BTC адресом НЕ найден. Транзакция не будет зачислена пользователю");
                        }
                    }
                }
                if (!exist_new_tx)
                    AsyncElectrumScheduleService.SetStatus("В Electrum нет ни одной новой транзакции");
                AsyncElectrumScheduleService.SetStatus(null);
                AsyncElectrumScheduleService.Transactions.Clear();
            }
        }

        public string GetFreeBitconAddress()
        {
            if (AsyncElectrumScheduleService.ElectrumClient != null)
            {
                SimpleStringArrayResponseClass addresses = (SimpleStringArrayResponseClass)AsyncElectrumScheduleService.ElectrumClient.GetListWalletAddresses();

                if (addresses?.result != null)
                {
                    List<string> electrum_addresses = addresses.result.ToList();
                    electrum_addresses.Sort();
                    foreach (string s in electrum_addresses)
                    {
                        if (busy_addresses.Contains(s))
                            continue;

                        AsyncElectrumScheduleService.SetStatus("Проверка адреса на прикрепление за пользователем: if (users.SingleOrDefault(x => x.BitcoinAddress == " + s + ") == null)");
                        if (db.Set<UserModel>().Count(x => x.BitcoinAddress == s) == 0)
                        {
                            AsyncElectrumScheduleService.SetStatus("Адресс " + s + " свободен");

                            busy_addresses.Add(s);
                            if (busy_addresses.Count > 50)
                                busy_addresses.RemoveRange(0, busy_addresses.Count - 30);

                            return s;
                        }
                    }
                }
            }

            return AsyncElectrumScheduleService?.ElectrumClient?.CreateNewAddress()?.result;
        }
    }
}
