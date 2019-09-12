////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
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
        public ElectrumJsonRpcSingletonAsyncScheduleService AsyncElectrumScheduleService { get; private set; }
        public ElectrumScopedSyncScheduleService(DbContext set_db, ElectrumJsonRpcSingletonAsyncScheduleService set_async_electrum_schedule_service)
            : base(set_db, set_async_electrum_schedule_service)
        {
            AsyncElectrumScheduleService = set_async_electrum_schedule_service;
        }

        public override void UpdateDataBase()
        {
            lock (AsyncElectrumScheduleService.Transactions)
            {
                foreach (TransactionWalletHistoryResponseClass TransactionWallet in AsyncElectrumScheduleService.Transactions)
                {
                    if (string.IsNullOrWhiteSpace(TransactionWallet.txid))
                    {
                        AsyncElectrumScheduleService.SetStatus("Прочитана транзакция из Electrum JSONRPC с пустым txid: " + TransactionWallet.ToString(), AbstractAsyncScheduler.StatusTypes.ErrorStatus);
                        AsyncElectrumScheduleService.SetStatus("Транзакция с пустым txid будет пропущена");
                        continue;
                    }

                    AsyncElectrumScheduleService.SetStatus("Поиск входящей транзакции в базе данных > SingleOrDefault(x => x.TxId == TransactionWallet.txid) // '" + TransactionWallet.txid + "'");
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
                        btcTransaction = new BtcTransactionModel()
                        {
                            TxId = TransactionWallet.txid,
                            Sum = glob_tools.GetDoubleFromString(TransactionWallet.value)
                        };

                        db.Add(btcTransaction);
                        db.SaveChanges();

                        foreach (TransactionWalletHistoryResponseOutputsClass TransactionOut in TransactionWallet.outputs.Where(x => AsyncElectrumScheduleService.ElectrumClient?.IsAddressMine(x.address)?.result == true))
                        {
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
                                btcTransactionOut.UserId = user.Id;
                                db.Update(btcTransactionOut);
                                //
                                ///int fiat_sum = (int)(btcTransactionOut.Sum * options.Value.CurrentBtcRate);
                                user.BalanceBTC += btcTransactionOut.Sum;
                                db.Update(user);

                                string notify = "Пополнение /balance +" + btcTransactionOut.Sum + "=" + user.BalanceBTC + " BTC";
                                db.Add(new MessageModel() { Information = notify, SenderId = null, RecipientId = user.Id, NeedTelegramNotify = user.TelegramId != 0 });

                                db.Add(new eCommerceJournalModel()
                                {
                                    BaseObjectId = btcTransactionOut.Id,
                                    TypeBaseObject = TypesBaseObject.TxOut,
                                    ClientId = user.Id,
                                    SumBTC = btcTransactionOut.Sum,
                                    Information = notify
                                });
                            }
                            db.SaveChanges();
                        }
                    }
                }
                AsyncElectrumScheduleService.SetStatus(null);
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
                        AsyncElectrumScheduleService.SetStatus("Проверка адреса на прикрепление за пользователем: if (users.SingleOrDefault(x => x.BitcoinAddress == "+s+") == null)");
                        if (db.Set<UserModel>().SingleOrDefault(x => x.BitcoinAddress == s) == null)
                        {
                            AsyncElectrumScheduleService.SetStatus("Адресс " + s + " свободен");
                            return s;
                        }
                    }
                }
            }

            return AsyncElectrumScheduleService.ElectrumClient.CreateNewAddress()?.result;
        }
    }
}
