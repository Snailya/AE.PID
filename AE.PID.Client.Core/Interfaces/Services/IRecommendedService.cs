using System.Collections.Generic;

namespace AE.PID.Client.Core;

public interface IRecommendedService
{
    /// <summary>
    ///     Temperately save the user selection to the memory, and send to the server in a batch to reduce network traffic.
    /// </summary>
    void Add(SelectionFeedback feedback);

    void AddRange(IEnumerable<SelectionFeedback> feedbacks);
}