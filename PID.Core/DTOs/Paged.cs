using System.Collections.Generic;

namespace AE.PID.Core.DTOs;

public class Paged<T>
{
    public int PageNo { get; set; }
    public int PageSize { get; set; }
    public int PagesCount { get; set; }

    public int ItemsCount { get; set; }
    public IEnumerable<T>? Items { get; set; }
}