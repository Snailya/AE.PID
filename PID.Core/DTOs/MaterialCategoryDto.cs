﻿using AE.PID.Core.Interfaces;

namespace AE.PID.Core.DTOs;

public class MaterialCategoryDto : ITreeNode
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Id { get; set; }
    public int ParentId { get; set; }
}