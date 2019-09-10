using AbstractSyncScheduler;
using LocalbitcoinsBtcRateSingletonAsyncScheduler;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Linq;

namespace LocalbitcoinsBtcRateScopedSyncScheduler
{
    public class LocalbitcoinsBtcRateScopedSyncScheduleService : BasicScopedSyncScheduler
    {
        public LocalbitcoinsBtcRateSingletonAsyncScheduleService AsyncLocalbitcoinsBtcRateScheduleService { get; private set; }

        public LocalbitcoinsBtcRateScopedSyncScheduleService(DbContext set_db, LocalbitcoinsBtcRateSingletonAsyncScheduleService set_async_localbitcoins_btc_rate_schedule_service)
            : base(set_db)
        {
            AsyncLocalbitcoinsBtcRateScheduleService = set_async_localbitcoins_btc_rate_schedule_service;

            lock (AsyncLocalbitcoinsBtcRateScheduleService)
            {
                if (AsyncLocalbitcoinsBtcRateScheduleService.SchedulerIsReady && AsyncLocalbitcoinsBtcRateScheduleService.RatesBTC.Count > 0)
                    UpdateDataBase();
            }
        }

        public override void UpdateDataBase()
        {
            lock (AsyncLocalbitcoinsBtcRateScheduleService.RatesBTC)
            {
                foreach (BtcRateLocalbitcoinsModel btcRate in AsyncLocalbitcoinsBtcRateScheduleService.RatesBTC.OrderBy(x => x.DateCreate))
                {
                    #region fantom error
                    // Каким то хером (core 2.2) тут возникает иногда фантомная ошибка.
                    // Сама собой появляется спустя некоторого количества наработки программы. Появялется откуда то ID 124 и не уходит:
                    // dbug: Microsoft.EntityFrameworkCore.Database.Command[20100]
                    //      Executing DbCommand [Parameters=[@p0='124', @p1='37', @p2='2019-09-02T15:53:50', @p3='Снимок состяния Localbitcoins qiwi' (Size = 4000), @p4='713997', @p5='710644.73', @p6='False', @p7='False', @p8='False'], CommandType='Text', CommandTimeout='30']
                    //      SET NOCOUNT ON;
                    //      INSERT INTO [BtcRatesLocalbitcoins] ([Id], [CountRates], [DateCreate], [Information], [MaxRate], [MinRate], [isDelete], [isFavorite], [isOff])
                    //      VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8);
                    //fail: Microsoft.EntityFrameworkCore.Database.Command[20102]
                    //      Failed executing DbCommand (1ms) [Parameters=[@p0='124', @p1='37', @p2='2019-09-02T15:53:50', @p3='Снимок состяния Localbitcoins qiwi' (Size = 4000), @p4='713997', @p5='710644.73', @p6='False', @p7='False', @p8='False'], CommandType='Text', CommandTimeout='30']
                    //      SET NOCOUNT ON;
                    //      INSERT INTO [BtcRatesLocalbitcoins] ([Id], [CountRates], [DateCreate], [Information], [MaxRate], [MinRate], [isDelete], [isFavorite], [isOff])
                    //      VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8);
                    //System.Data.SqlClient.SqlException (0x80131904): Cannot insert explicit value for identity column in table 'BtcRatesLocalbitcoins' when IDENTITY_INSERT is set to OFF.
                    //at System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
                    //at System.Data.SqlClient.SqlInternalConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
                    //at System.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, Boolean callerHasConnectionLock, Boolean asyncClose)
                    //at System.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)
                    //at System.Data.SqlClient.SqlDataReader.TryConsumeMetaData()
                    //at System.Data.SqlClient.SqlDataReader.get_MetaData()
                    //at System.Data.SqlClient.SqlCommand.FinishExecuteReader(SqlDataReader ds, RunBehavior runBehavior, String resetOptionsString)
                    //at System.Data.SqlClient.SqlCommand.RunExecuteReaderTds(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, Boolean async, Int32 timeout, Task& task, Boolean asyncWrite, SqlDataReader ds)
                    //at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, TaskCompletionSource`1 completion, Int32 timeout, Task& task, Boolean asyncWrite, String method)
                    //at System.Data.SqlClient.SqlCommand.ExecuteReader(CommandBehavior behavior)
                    //at System.Data.SqlClient.SqlCommand.ExecuteDbDataReader(CommandBehavior behavior)
                    //at System.Data.Common.DbCommand.ExecuteReader()
                    //at Microsoft.EntityFrameworkCore.Storage.Internal.RelationalCommand.Execute(IRelationalConnection connection, DbCommandMethod executeMethod, IReadOnlyDictionary`2 parameterValues)
                    //ClientConnectionId:a82f4926-e0d4-4e2b-b4d6-4472cb3080bf
                    //Error Number:544,State:1,Class:16
                    #endregion
                    btcRate.Id = 0;

                    db.Add(btcRate);
                    db.SaveChanges();
                }
                AsyncLocalbitcoinsBtcRateScheduleService.RatesBTC = new ConcurrentBag<BtcRateLocalbitcoinsModel>();
            }
            //if (AsyncScheduleService.LastChangeStatusDateTime.AddSeconds(AsyncScheduleService.SchedulePausePeriod) < DateTime.Now)
            //    AsyncScheduleService.InvokeAsyncSchedule();
        }
    }
}
