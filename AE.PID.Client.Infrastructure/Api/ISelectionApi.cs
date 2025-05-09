﻿using System.Threading.Tasks;
using AE.PID.Core;
using Refit;

namespace AE.PID.Client.Infrastructure;

public interface ISelectionApi
{
    [Post("/api/v3/selections")]
    Task SaveAsync(UserMaterialSelectionFeedbackDto[] selections);
}