using System.Collections.Concurrent;
using System.Data.Odbc;

namespace OracleOdbcApi.Transactions
{
    public class TransactionSessionManager
    {
        private readonly ConcurrentDictionary<string, (OdbcConnection conn, OdbcTransaction tx)> _sessions = new();

        public string StartSession(string connectionString)
        {
            var conn = new OdbcConnection(connectionString);
            conn.Open();
            var tx = conn.BeginTransaction();
            var sessionId = Guid.NewGuid().ToString();
            _sessions[sessionId] = (conn, tx);
            return sessionId;
        }

        public (OdbcConnection conn, OdbcTransaction tx)? GetSession(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }

        public void Commit(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.tx.Commit();
                session.conn.Close();
            }
        }

        public void Rollback(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.tx.Rollback();
                session.conn.Close();
            }
        }
    }
}
