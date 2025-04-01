using AE.PID.Client.Core;
using AE.PID.Core;
using AE.PID.Core.DTOs;

namespace AE.PID.Client.Infrastructure;

public static class DtoExt
{
    public static Project ToProject(this ProjectDto dto)
    {
        return
            new Project
            {
                Id = dto.Id,
                Name = dto.Name,
                Code = dto.Code,
                FamilyName = dto.FamilyName,
                // todo: not initialized
                Director = string.Empty,
                ProjectManager = string.Empty
            };
    }

    public static Function ToFunction(this FunctionDto dto)
    {
        return new Function
        {
            Id = dto.Id,
            Type = dto.FunctionType,
            Code = dto.Code,
            Name = dto.Name,
            EnglishName = dto.EnglishName,
            Description = dto.Description
        };
    }
}