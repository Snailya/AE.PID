using System;
using System.Collections.Generic;
using System.Linq;
using AE.PID.Core.DTOs;
using AE.PID.Core.Models;
using AE.PID.Visio.Core.Interfaces;
using AE.PID.Visio.Core.Models;
using Splat;

namespace AE.PID.Visio.Shared.Services;

public class RecommendedService(IApiFactory<ISelectionApi> apiFactory, IProjectStore projectStore)
    : IRecommendedService, IDisposable, IEnableLogger
{
    private readonly List<SelectionFeedback> _feedbacks = [];

    public void Dispose()
    {
        if (_feedbacks.Count <= 0) return;
        var projectId = projectStore.GetCurrentProject()?.Id;

        apiFactory.Api.SaveAsync(_feedbacks.Select(x => new UserMaterialSelectionFeedbackDto
        {
            MaterialId = 0,
            MaterialLocationContext = new MaterialLocationContext
            {
                ProjectId = projectId,
                FunctionZone = x.FunctionZone,
                FunctionGroup = x.FunctionGroup,
                FunctionElement = x.FunctionElement,
                MaterialLocationType = x.MaterialLocationType
            },
            RecommendationCollectionId = null,
            SelectedRecommendationId = null
        }).ToArray());
    }

    public void Add(SelectionFeedback feedback)
    {
        _feedbacks.Add(feedback);
    }

    public void AddRange(IEnumerable<SelectionFeedback> feedbacks)
    {
        _feedbacks.AddRange(feedbacks);
    }
}