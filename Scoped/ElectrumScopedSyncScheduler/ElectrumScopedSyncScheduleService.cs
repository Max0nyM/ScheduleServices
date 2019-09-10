////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using AbstractSyncScheduler;
using ElectrumSingletonAsyncSheduler;
using Microsoft.EntityFrameworkCore;

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
            //throw new NotImplementedException();
        }
    }
}
