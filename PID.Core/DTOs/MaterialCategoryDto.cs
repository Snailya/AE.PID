﻿namespace AE.PID.Core.DTOs;

public class MaterialCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int ParentId { get; set; }
}