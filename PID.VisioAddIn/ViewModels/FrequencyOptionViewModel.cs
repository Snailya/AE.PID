using System;
using System.Linq;

namespace AE.PID.ViewModels;

public class FrequencyOptionViewModel
{
    public static FrequencyOptionViewModel[] Options =
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

    public string Label { get; set; } = string.Empty;
    public TimeSpan TimeSpan { get; set; }


    public static FrequencyOptionViewModel GetMatchedOption(TimeSpan timeSpan)
    {
        return Options
            .OrderBy(x => Math.Abs(timeSpan.Ticks - x.TimeSpan.Ticks))
            .First();
    }
}