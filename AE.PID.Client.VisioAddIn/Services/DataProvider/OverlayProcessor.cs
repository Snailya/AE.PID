using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using AE.PID.Client.Core;
using AE.PID.Client.Core.VisioExt;
using AE.PID.Core;
using DynamicData;
using Microsoft.Office.Interop.Visio;
using Splat;

namespace AE.PID.Client.VisioAddIn;

internal class OverlayProcessor : IEnableLogger
{
    private const string SolutionXmlKey = "location-overlay";

    private readonly SourceCache<LocationOverlay, VirtualLocationKey> _cache = new(x => x.Key);

    private readonly Document _document;

    public OverlayProcessor(Document document, IScheduler? scheduler = null)
    {
        _document = document;

        Initialize();

        // todo: 定时刷新
    }

    public IObservableCache<LocationOverlay, VirtualLocationKey> Cache => _cache.AsObservableCache();


    private void Initialize()
    {
        RefreshCache();
    }

    private void RefreshCache()
    {
        if (_document.SolutionXMLElementExists[SolutionXmlKey])
            try
            {
                var updates = SolutionXmlHelper.Get<List<LocationOverlay>>(_document, SolutionXmlKey);

                _cache.Edit(updater =>
                {
                    updater.Clear();
                    updater.AddOrUpdate(updates);
                });
            }
            catch (Exception e)
            {
                this.Log().Error("Failed to read location overlay from solution xml.", e);
            }
    }
    
    public void Write(LocationOverlay[] locations)
    {
        _cache.Edit(updater =>
        {
            foreach (var location in locations)
                if (location.IsEmpty)
                    updater.Remove(location);
                else
                    updater.AddOrUpdate(locations);
        });

        SolutionXmlHelper.Store(_document, new SolutionXmlElement<List<LocationOverlay>>
        {
            Name = SolutionXmlKey,
            Data = [.. _cache.Items]
        });
    }

    public static FunctionLocation ApplyOverlay(FunctionLocation source, LocationOverlay overlay)
    {
        return source with
        {
            Description = overlay.Description ?? source.Description,
            UnitMultiplier = overlay.UnitMultiplier ?? source.UnitMultiplier
        };
    }

    public static MaterialLocation ApplyOverlay(MaterialLocation source,
        LocationOverlay overlay)
    {
        return source with
        {
            Quantity = overlay.Quantity ?? source.Quantity,
            Code = overlay.Code ?? source.Code,
            UnitMultiplier = overlay.UnitMultiplier ?? source.UnitMultiplier
        };
    }
}