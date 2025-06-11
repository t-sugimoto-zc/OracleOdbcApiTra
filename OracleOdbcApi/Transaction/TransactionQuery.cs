namespace OracleOdbcApi.Transactions
{
    public class TransactionQuery
    {
        public string SessionId { get; set; } = string.Empty;
        public string Sql { get; set; } = string.Empty;
    }
}
