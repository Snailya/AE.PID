using AE.PID.Core.DTOs;
using AE.PID.Server.Data;

namespace AE.PID.Server;

public static class DtoExtensions
{
    public static DetailedLibraryItemDto ToDetailedLibraryItemDto(this LibraryItem x)
    {
        return new DetailedLibraryItemDto
        {
            Name = x.Name,
            UniqueId = x.UniqueId,
            BaseId = x.BaseId,
            LineStyleName = x.LibraryVersionItemXML.LineStyleName,
            FillStyleName = x.LibraryVersionItemXML.FillStyleName,
            TextStyleName = x.LibraryVersionItemXML.TextStyleName,
            MasterElement = x.LibraryVersionItemXML.MasterElement,
            MasterDocument = x.LibraryVersionItemXML.MasterDocument
        };
    }
}