namespace AE.PID.Core;

public interface IPageResponse
{
    int Page { get; set; }

    int Pages { get; set; }

    int TotalSize { get; set; }

    int PageSize { get; set; }
}