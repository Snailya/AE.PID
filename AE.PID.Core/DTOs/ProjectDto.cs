﻿namespace AE.PID.Visio.Core.DTOs;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;
}