using System;
using System.Collections.Generic;
using System.Linq;
using AE.PID.Client.Core;
using AE.PID.Core;
using Splat;

namespace AE.PID.Client.Infrastructure;

public class RecommendedService(IApiFactory<ISelectionApi> apiFactory, IProjectLocationStore projectLocationStore)
    : IRecommendedService, IDisposable, IEnableLogger
{
    private readonly List<SelectionFeedback> _feedbacks = [];

    public void Dispose()
    {
        if (_feedbacks.Count <= 0) return;

        apiFactory.Api.SaveAsync(_feedbacks.Select(x => new UserMaterialSelectionFeedbackDto
        {
            MaterialId = 0,
            MaterialLocationContext = new MaterialLocationContext
            {
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