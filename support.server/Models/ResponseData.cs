namespace support.server.Models
{
    public class ApiResponse<T>
    {
        public int status { get; set; }
        public string message { get; set; }
        public T Data { get; set; }
    }
}
