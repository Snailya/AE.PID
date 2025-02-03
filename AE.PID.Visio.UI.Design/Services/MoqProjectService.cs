using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Client.Core;
using AE.PID.Core.DTOs;
using AE.PID.Core.Models;
using DynamicData;

namespace AE.PID.Visio.UI.Design;

internal sealed class MoqProjectService : IProjectService
{
    private readonly List<Project> _projects =
    [
        new()
        {
            Id = 1,
            Name = "Project 1",
            Code = "1",
            FamilyName = "Project Family 1"
        },

        new()
        {
            Id = 2,
            Name = "Project 2",
            Code = "2",
            FamilyName = "Project Family 2"
        },

        new()
        {
            Id = 3,
            Name = "Project 3",
            Code = "3",
            FamilyName = "Project Family 3"
        }
    ];

    public Task<Paged<Project>> GetAllAsync(string searchTerm, PageRequest pageRequest,
        CancellationToken token = default)
    {
        return Task.FromResult(new Paged<Project>
            { Items = _projects, Page = 1, Pages = 1, PageSize = _projects.Count, TotalSize = _projects.Count });
    }

    public Task<Project> GetByIdAsync(int id)
    {
        var project = _projects.SingleOrDefault(x => x.Id == id);
        if (project != null) return Task.FromResult(project);

        throw new KeyNotFoundException();
    }
}

internal sealed class MoqMaterialService : IMaterialService
{
    private readonly List<MaterialCategory> _categories =
    [
        new()
        {
            Id = 1,
            ParentId = 0,
            Name = "Category 1"
        }
    ];

    private readonly List<Material> _materials;

    public MoqMaterialService()
    {
        _materials =
        [
            new Material
            {
                Id = 1,
                Code = "1",
                Name = "Material 1",
                Category = _categories[0],
                Properties =
                [
                    new MaterialProperty
                    {
                        Name = "Property 1",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 2",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 3",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 4",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 5",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 6",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 7",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 8",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 9",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 10",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 11",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 12",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    },
                    new MaterialProperty
                    {
                        Name = "Property 13",
                        Value =
                            "This is for material property display test, you can see this sentence is extremely long, so I can test whether it can display properly"
                    }
                ]
            },
            new Material
            {
                Id = 2,
                Code = "2",
                Name = "Material 2",
                Category = _categories[0]
            },
            new Material
            {
                Id = 3,
                Code = "3",
                Name = "Material 3",
                Category = _categories[0]
            }
        ];
    }

    public Task<IEnumerable<MaterialCategory>> GetCategoriesAsync()
    {
        return Task.FromResult(_categories.AsEnumerable());
    }

    public Task<Paged<Material>> GetAsync(int? categoryId, PageRequest pageRequest, CancellationToken token = default)
    {
        var materials = _materials.Where(x => x.Category.Id == categoryId).ToList();
        return Task.FromResult(new Paged<Material>
        {
            Items = materials,
            Page = 1,
            Pages = 1,
            TotalSize = materials.Count,
            PageSize = materials.Count
        });
    }

    public Task<Paged<Material>> SearchAsync(string s, int? categoryId, PageRequest pageRequest,
        CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<Material> GetByCodeAsync(string? code, CancellationToken token = default)
    {
        return Task.FromResult(_materials.SingleOrDefault(x => x.Code == code));
    }

    public Task<Dictionary<string, string[]>> GetCategoryMapAsync()
    {
        return Task.FromResult(new Dictionary<string, string[]>());
    }

    public Task<IEnumerable<Recommendation<Material>>> GetRecommendationAsync(MaterialLocationContext context,
        CancellationToken token = default)
    {
        return Task.FromResult(Enumerable.Empty<Recommendation<Material>>());
    }

    public Task FeedbackAsync(MaterialLocationContext context, int materialId, int? collectionId = null,
        int? recommendationId = null)
    {
        return Task.CompletedTask;
    }
}