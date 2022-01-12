namespace NjuCsCmsHelper.Server;

public class HttpResponseException : Exception
{
    public int Status { get; set; }
    public object Value { get; set; }

    public HttpResponseException(int Status, object Value)
    {
        this.Status = Status;
        this.Value = Value;
    }
}
