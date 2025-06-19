using Microsoft.AspNetCore.Mvc;
using OracleOdbcApi.Transactions;
using System.Data.Odbc;

namespace OracleOdbcApi.Transactions
{
  [ApiController]
    [Route("[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly TransactionSessionManager _sessionManager;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(IConfiguration config, TransactionSessionManager sessionManager, ILogger<TransactionController> logger)
        {
            _config = config;
            _sessionManager = sessionManager;
            _logger = logger;
        }

        [HttpPost("start")]
        public IActionResult StartTransaction()
        {
            var podName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown";
            _logger.LogInformation($"StartTransaction called on pod: {podName}");

            var connStr = _config.GetConnectionString("OracleOdbc");
            if (string.IsNullOrWhiteSpace(connStr))
                return StatusCode(500, new { error = "接続文字列が設定されていません。" });

            var sessionId = _sessionManager.StartSession(connStr);
            return Ok(new { sessionId });
        }

        [HttpPost("query")]
        public IActionResult ExecuteQuery([FromBody] TransactionQuery query)
        {
            var podName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown";
            _logger.LogInformation($"StartTransaction called on pod: {podName}");

            var session = _sessionManager.GetSession(query.SessionId);
            if (session == null)
                return BadRequest(new { error = "無効なセッションIDです。" });

            try
            {
                using var cmd = new OdbcCommand(query.Sql, session.Value.conn, session.Value.tx);
                using var reader = cmd.ExecuteReader();
                var results = new List<Dictionary<string, object>>();

                while (reader.Read())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                    }
                    results.Add(row); 
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "SQL実行中にエラーが発生しました。", message = ex.Message });
            }
        }

        [HttpPost("commit")]
        public IActionResult Commit([FromBody] SessionRequest request)
        {
            var podName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown";
            _logger.LogInformation($"StartTransaction called on pod: {podName}");
            
            _sessionManager.Commit(request.SessionId);
            return Ok(new { message = "コミットしました。" });
        }

        [HttpPost("rollback")]
        public IActionResult Rollback([FromBody] SessionRequest request)
        {
            var podName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown";
            _logger.LogInformation($"StartTransaction called on pod: {podName}");
            
            _sessionManager.Rollback(request.SessionId);
            return Ok(new { message = "ロールバックしました。" });
        }
    }
}
