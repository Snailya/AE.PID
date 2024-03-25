using AE.PID.Core.DTOs;
using AE.PID.Server.Data;

namespace AE.PID.Server;

public static class DtoExtensions
{
    public static DetailedLibraryItemDto ToDetailedLibraryItemDto(this LibraryItemEntity x)
    {
        return new DetailedLibraryItemDto
        {
            Name = x.Name,
            UniqueId = x.UniqueId,
            BaseId = x.BaseId,
            LineStyleName = x.LineStyleName,
            FillStyleName = x.FillStyleName,
            TextStyleName = x.TextStyleName,
            MasterElement = x.MasterElement,
            MasterDocument = x.MasterDocument
        };
    }
}