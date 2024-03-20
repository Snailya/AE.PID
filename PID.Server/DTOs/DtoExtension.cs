using System;
using System.Linq;
using AE.PID.Core.DTOs;
using AE.PID.Server.DTOs.PDMS;

namespace AE.PID.Server.DTOs;

public static class DtoExtension
{
    public static MaterialCategoryDto FromPDMS(this SelectDesignMaterialCategoryResponseItemDto dto)
    {
        return new MaterialCategoryDto
        {
            Id = Convert.ToInt32(dto.MainTable.Id),
            Name = dto.MainTable.CategoryName,
            Code = dto.MainTable.CategoryCode,
            ParentId = int.TryParse(dto.MainTable.ParentId, out var parentId) ? parentId : default
        };
    }

    public static MaterialDto FromPDMS(this SelectDesignMaterialResponseItemDto dto)
    {
        return new MaterialDto
        {
            Id = int.TryParse(dto.MainTable.Id, out var id) ? id : default,
            Brand = dto.MainTable.Brand,
            // todo: get parent categories
            Categories = [int.TryParse(dto.MainTable.MaterialCategory, out var categoryId) ? categoryId : default],
            Code = dto.MainTable.MaterialCode,
            Description = dto.MainTable.Description,
            Manufacturer = dto.MainTable.Manufacturer,
            Model = dto.MainTable.Model,
            ManufacturerMaterialNumber = dto.MainTable.ManufacturerMaterialNumber,
            Name = dto.MainTable.MaterialName,
            Properties = dto.Detail1.Select(x => new MaterialPropertyDto()
                { Id = x.Id, Name = x.Name, Value = x.Value }),
            Specifications = dto.MainTable.Specifications,
            Type = dto.MainTable.MaterialType,
            Unit = dto.MainTable.Unit
        };
    }

    public static ProjectDto FromPDMS(this SelectNewProjectInfoResponseItemDto dto)
    {
        return new ProjectDto()
        {
            Id = dto.MainTable.Id,
            Code = dto.MainTable.ProjectCode,
            Name = dto.MainTable.ProjectName
        };
    }
}