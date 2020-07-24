using Dapper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace b_118.Database
{
    class B118DB
    {
        private readonly DatabaseConnection _dbConnection;
        private bool hasInitialized = false;

        public B118DB(string filename)
        {
            _dbConnection = new DatabaseConnection(filename);
        }

        public async Task<bool> Init()
        {
            if (!hasInitialized)
            {
                hasInitialized = await _dbConnection.Init(
                    @"create table if not exists inviteblacklist (userid int, roleid int);
                      create table if not exists requestblacklist (userid int, roleid int);"
                );
            }
            return hasInitialized;
        }

        public async Task<int> CountBlackListedInvites()
        {
            if (!hasInitialized)
                throw new InvalidOperationException("B118DB must be initalized first.");
            string sql = "select count(*) from inviteblacklist";
            var results = await _dbConnection.GetConnection().QueryFirstAsync<int>(sql);
            return results;
        }

        public async Task<int> BlackListInvite(ulong userid, ulong roleid)
        {
            if (!hasInitialized)
                throw new InvalidOperationException("B118DB must be initialized first.");
            string sql = "insert into inviteblacklist (userid, roleid) values (@userid, @roleid);";
            var param = new
            {
                userid,
                roleid
            };
            var results = await _dbConnection.GetConnection().ExecuteAsync(sql, param);
            return results;
        }

        public async Task<int> UnblacklistInvite(ulong userid, ulong roleid)
        {
            if (!hasInitialized)
                throw new InvalidOperationException("B118DB must be initialized first.");
            string sql = "delete from inviteblacklist where userid = @userid and roleid = @roleid;";
            var param = new
            {
                userid,
                roleid
            };
            return await _dbConnection.GetConnection().ExecuteAsync(sql, param);
        }

        public async Task<IEnumerable<int>> GetInviteBlacklistForUser(ulong userid)
        {
            if (!hasInitialized)
                throw new InvalidOperationException("B118DB must be initialized first.");
            string sql = "select roleid from inviteblacklist where userid = @userid";
            var param = new
            {
                userid
            };
            return await _dbConnection.GetConnection().QueryAsync<int>(sql, param);
        }

        public async Task<int> BlackListRequest(ulong userid, ulong roleid)
        {
            if (!hasInitialized)
                throw new InvalidOperationException("B118DB must be initialized first.");
            string sql = "insert into requestblacklist (userid, roleid) values (@userid, @roleid);";
            var param = new
            {
                userid,
                roleid
            };
            return await _dbConnection.GetConnection().ExecuteAsync(sql, param);
        }

        public async Task<int> UnblacklistRequest(ulong userid, ulong roleid)
        {
            if (!hasInitialized)
                throw new InvalidOperationException("B118DB must be initialized first.");
            string sql = "delete from requestblacklist where userid = @userid and roleid = @roleid;";
            var param = new
            {
                userid,
                roleid
            };
            return await _dbConnection.GetConnection().ExecuteAsync(sql, param);
        }

        public async Task<IEnumerable<int>> GetInviteBlacklistForCampaign(ulong roleid)
        {
            if (!hasInitialized)
                throw new InvalidOperationException("B118DB must be initialized first.");
            string sql = "select userid from requestblacklist where roleid = @roleid";
            var param = new
            {
                roleid
            };
            return await _dbConnection.GetConnection().QueryAsync<int>(sql, param);
        }
    }
}
