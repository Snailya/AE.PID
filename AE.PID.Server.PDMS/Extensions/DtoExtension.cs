using AE.PID.Core.DTOs;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.DTOs;

namespace AE.PID.Server.PDMS.Extensions;

public static class DtoExtension
{
    public static MaterialCategoryDto FromPDMS(this SelectDesignMaterialCategoryResponseItemDto dto)
    {
        return new MaterialCategoryDto
        {
            Id = Convert.ToInt32(dto.MainTable.Id),
            NodeName = dto.MainTable.CategoryName,
            Code = dto.MainTable.Code,
            ParentId = int.TryParse(dto.MainTable.ParentId, out var parentId) ? parentId : default
        };
    }

    public static MaterialDto FromPDMS(this SelectDesignMaterialResponseItemDto dto)
    {
        return new MaterialDto
        {
            Id = Convert.ToInt32(dto.MainTable.Id),
            Brand = dto.MainTable.Brand,
            // todo: get parent categories
            Categories = [int.TryParse(dto.MainTable.MaterialCategory, out var categoryId) ? categoryId : default],
            Code = dto.MainTable.MaterialCode,
            Description = dto.MainTable.Description,
            Manufacturer = dto.MainTable.Manufacturer,
            Model = dto.MainTable.Model,
            ManufacturerMaterialNumber = dto.MainTable.ManufacturerMaterialNumber,
            Name = dto.MainTable.MaterialName,
            Properties = dto.Detail1.Select(x => new MaterialPropertyDto { Id = x.Id, Name = x.Name, Value = x.Value }),
            Specifications = dto.MainTable.Specifications,
            Type = dto.MainTable.MaterialType,
            Unit = dto.MainTable.Unit
        };
    }

    public static ProjectDto FromPDMS(this SelectNewProjectInfoResponseItemDto dto)
    {
        return new ProjectDto
        {
            Id = Convert.ToInt32(dto.MainTable.Id),
            Code = dto.MainTable.ProjectCode,
            Name = dto.MainTable.ProjectName,
            FamilyName = dto.MainTable.FamilyId
        };
    }

    public static FunctionDto FromPDMS(this SelectProjectProcessSectionResponseItemDto dto)
    {
        return new FunctionDto
        {
            Id = Convert.ToInt32(dto.MainTable.Id),
            Code = dto.MainTable.Code,
            Name = dto.MainTable.Name,
            EnglishName = dto.MainTable.EnglishName,
            Description = dto.MainTable.Description
        };
    }

    public static FunctionDto FromPDMS(this SelectProjectFunctionGroupResponseItemDto dto)
    {
        return new FunctionDto
        {
            Id = Convert.ToInt32(dto.MainTable.Id),
            Code = dto.MainTable.Code,
            Name = dto.MainTable.Name,
            EnglishName = dto.MainTable.EnglishName,
            Description = dto.MainTable.Description,
            IsEnabled = int.TryParse(dto.MainTable.IsEnabled, out var isEnabled) ? isEnabled == 1 : default,
            FunctionType = FunctionType.FunctionGroup
        };
    }

    public static FunctionDto FromPDMS(this SelectFunctionGroupResponseItemDto dto)
    {
        return new FunctionDto
        {
            Id = Convert.ToInt32(dto.MainTable.Id),
            Code = dto.MainTable.Code,
            Name = dto.MainTable.Name,
            EnglishName = dto.MainTable.EnglishName,
            IsEnabled = int.TryParse(dto.MainTable.IsEnabled, out var isEnabled) && isEnabled == 1,
            FunctionType = FunctionType.FunctionGroup
        };
    }
}