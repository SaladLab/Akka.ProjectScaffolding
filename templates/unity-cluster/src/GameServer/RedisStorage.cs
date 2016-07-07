using Domain;
using StackExchange.Redis;
using TrackableData.Redis;

namespace GameServer
{
    public class RedisStorage
    {
        public static RedisStorage Instance { get; set; }

        public static IDatabase Db
        {
            get { return Instance.GetDatabase(); }
        }

        public static TrackableContainerHashesRedisMapper<IUserContext> UserContextMapper =
            new TrackableContainerHashesRedisMapper<IUserContext>();

        private ConnectionMultiplexer _connection;

        public RedisStorage(string configuration)
        {
            _connection = ConnectionMultiplexer.Connect(configuration);
        }

        public IDatabase GetDatabase(int db = -1)
        {
            return _connection.GetDatabase(db);
        }
    }
}
