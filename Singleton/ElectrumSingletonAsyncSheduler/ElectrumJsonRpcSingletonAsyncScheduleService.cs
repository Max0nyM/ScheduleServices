////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using AbstractAsyncScheduler;
using ElectrumJSONRPC;
using ElectrumJSONRPC.Response.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ElectrumSingletonAsyncSheduler
{
    public class ElectrumJsonRpcSingletonAsyncScheduleService : BasicSingletonScheduler
    {
        public string JsonRpcUsername { get; set; }
        public string JsonRpcPassword { get; set; }
        public string JsonRpcServerAddress { get; set; }
        public int JsonRpcServerPort { get; set; }

        public override bool SchedulerIsReady => !string.IsNullOrEmpty(ElectrumVersion) && base.SchedulerIsReady;

        public string ElectrumVersion { get; set; }
        public ConcurrentBag<TransactionWalletHistoryResponseClass> Transactions = new ConcurrentBag<TransactionWalletHistoryResponseClass>();
        public ConcurrentBag<string> AddressesWalet = new ConcurrentBag<string>();
        public int TransactionsHistoryFromHeight { get; private set; }

        public Electrum_JSONRPC_Client ElectrumClient { get; private set; }

        public ElectrumJsonRpcSingletonAsyncScheduleService(ILoggerFactory set_logger_factory, string set_json_rpc_username, string set_json_rpc_password, string set_json_rpc_server_address, int set_json_rpc_server_port, int set_schedule_pause_period)
            : base(set_logger_factory, set_schedule_pause_period)
        {
            JsonRpcUsername = set_json_rpc_username;
            JsonRpcPassword = set_json_rpc_password;
            JsonRpcServerAddress = set_json_rpc_server_address;
            JsonRpcServerPort = set_json_rpc_server_port;
            ElectrumClient = new Electrum_JSONRPC_Client(JsonRpcUsername, JsonRpcPassword);
        }

        private async void ConnectToJsonRpcAsync()
        {
            SetStatus("Проверка версии Electrum: " + GetType().Name);
            await Task.Run(() =>
            {
                try
                {
                    SimpleStringResponseClass electrum_version = ElectrumClient.GetElectrumVersion();
                    ElectrumVersion = electrum_version?.result;
                }
                catch (Exception e)
                {

                    SetStatus("Ошибка чтения данных Electrum JSONRPC: " + e.Message, StatusTypes.ErrorStatus);
                    SetStatus("jsonrpc_response_raw: " + ElectrumClient.jsonrpc_response_raw, StatusTypes.ErrorStatus);
                    SetStatus("HttpStatusCode: " + ElectrumClient.CurrentHttpStatusCode, StatusTypes.ErrorStatus);
                    SetStatus("HttpStatusDescription: " + ElectrumClient.CurrentStatusDescription, StatusTypes.ErrorStatus);
                    SetStatus(null);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ElectrumVersion))
                {
                    SetStatus("Ошибка загрузки Electrum кошелька. ElectrumVersion is NULL", StatusTypes.ErrorStatus);
                    SetStatus(null);
                    return;
                }
                SetStatus("Electrum [ver." + ElectrumVersion + "] is ready...", StatusTypes.ErrorStatus);
                AsyncScheduleAction();
            });
        }

        protected override void AsyncScheduleAction()
        {
            WalletTransactionsHistoryResponseClass transactions = ElectrumClient.GetTransactionsHistoryWallet(true, true, true, null, TransactionsHistoryFromHeight);
            if (transactions is null)
            {
                SetStatus("Ошибка загрузки транзакций Electrum. transactions is null", StatusTypes.ErrorStatus);
                SetStatus(null);
                return;
            }
            if (transactions.error != null)
            {
                SetStatus("Ошибка загрузки транзакций Electrum. transactions.error != null: [code:" + transactions.error.code + "][message:" + transactions.error.message + "]", StatusTypes.ErrorStatus);
                SetStatus(null);
                return;
            }
            if (transactions.result?.transactions == null)
            {
                SetStatus("Ошибка загрузки транзакций Electrum. transactions.result?.transactions == null", StatusTypes.ErrorStatus);
                SetStatus(null);
                return;
            }
            if (transactions.result.transactions.Length == 0)
            {
                SetStatus("Ошибка загрузки транзакций Electrum. transactions.result.transactions.Length == 0", StatusTypes.ErrorStatus);
                SetStatus(null);
                return;
            }
            lock (Transactions)
            {
                Transactions = new ConcurrentBag<TransactionWalletHistoryResponseClass>(transactions.result.transactions);
            }

            lock (AddressesWalet)
            {
                if (AddressesWalet.Count == 0)
                {
                    try
                    {
                        string[] walet_addresses = ElectrumClient.GetListWalletAddresses().result;
                        if (walet_addresses != null && walet_addresses.Length > 0)
                            AddressesWalet = new ConcurrentBag<string>(walet_addresses);
                        else
                        {
                            SetStatus("Ошибка загрузки доступных адресов кошелька Electrum. Запрос доступных адресов пустой", StatusTypes.ErrorStatus);
                            SetStatus(null);
                            return;
                        }

                    }
                    catch (Exception e)
                    {
                        SetStatus("Внутреннее исключение загрузки доступных адресов кошелька Electrum: " + e.Message, StatusTypes.ErrorStatus);
                        SetStatus(null);
                        return;
                    }
                }
            }
        }

        public override void InvokeAsyncSchedule()
        {
            if (string.IsNullOrWhiteSpace(ElectrumVersion))
            {
                SetStatus("Ошибка загрузки Electrum кошелька. ElectrumVersion is NULL", StatusTypes.ErrorStatus);
                SetStatus(null);
                return;
            }
            base.InvokeAsyncSchedule();
        }
    }
}
