
namespace WebPerformanceCalculator.Shared.Types
{
    public class WorkerDataModel
    {
        public string? Data { get; set; }

        public DataType DataType { get; set; }
    }

    public enum DataType
    {
        Profile,
        Map,
        Update
    }
}
