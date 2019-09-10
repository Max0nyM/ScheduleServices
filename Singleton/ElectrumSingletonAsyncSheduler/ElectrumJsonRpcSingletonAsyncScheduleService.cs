﻿using AbstractAsyncScheduler;
using Microsoft.Extensions.Logging;

namespace ElectrumSingletonAsyncSheduler
{
    public class ElectrumJsonRpcSingletonAsyncScheduleService : BasicSingletonScheduler
    {
        public string JsonRpcUsername { get; set; }
        public string JsonRpcPassword { get; set; }
        public string JsonRpcServerAddress { get; set; }
        public int JsonRpcServerPort { get; set; }

        public ElectrumJsonRpcSingletonAsyncScheduleService(ILoggerFactory loggerFactory, string set_json_rpc_username, string set_json_rpc_password, string set_json_rpc_server_address, int set_json_rpc_server_port) : base(loggerFactory)
        {
            JsonRpcUsername = set_json_rpc_username;
            JsonRpcPassword = set_json_rpc_password;
            JsonRpcServerAddress = set_json_rpc_server_address;
            JsonRpcServerPort = set_json_rpc_server_port;
        }

        protected override void AsyncScheduleAction()
        {
            // throw new System.NotImplementedException();
        }
    }
}