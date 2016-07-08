using System;
using System.Configuration;

namespace GameServer
{
    public class RedisStorageFixture : IDisposable
    {
        public RedisStorageFixture()
        {
            var cstr = ConfigurationManager.ConnectionStrings["Redis"].ConnectionString;
            RedisStorage.Instance = new RedisStorage(cstr);
        }

        public void Dispose()
        {
            RedisStorage.Instance = null;
        }
    }
}
