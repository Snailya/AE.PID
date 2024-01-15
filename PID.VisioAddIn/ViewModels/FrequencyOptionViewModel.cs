using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;

namespace AE.PID.ViewModels;

public class FrequencyOptionViewModel : ReactiveObject
{
    public string Label { get; set; }
    public TimeSpan TimeSpan { get; set; }

    public static IEnumerable<FrequencyOptionViewModel> GetOptions()
    {
        return
        [
            new FrequencyOptionViewModel
            {
                Label = "每小时",
                TimeSpan = TimeSpan.FromHours(1)
            },
            new FrequencyOptionViewModel
            {
                Label = "每天",
                TimeSpan = TimeSpan.FromDays(1)
            },
            new FrequencyOptionViewModel
            {
                Label = "每周",
                TimeSpan = TimeSpan.FromDays(7)
            }
        ];
    }
}