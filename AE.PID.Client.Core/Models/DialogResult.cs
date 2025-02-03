namespace AE.PID.Client.Core;

public class DialogResult<T>
{
    public bool Result { get; set; }
    public T? Data { get; set; }

    public static DialogResult<T> Ok(T data)
    {
        return new DialogResult<T> { Result = true, Data = data };
    }

    public static DialogResult<T> Cancel()
    {
        return new DialogResult<T>();
    }
}