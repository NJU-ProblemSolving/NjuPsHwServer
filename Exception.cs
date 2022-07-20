namespace NjuCsCmsHelper.Server;

public class HttpResponseException : Exception
{
    public int Status { get; set; }
    public object Value { get; set; } = null!;

    public HttpResponseException(int Status, object Value)
    {
        this.Status = Status;
        this.Value = Value;
    }

    public HttpResponseException()
    {
    }

    public HttpResponseException(string message) : base(message)
    {
    }

    public HttpResponseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
