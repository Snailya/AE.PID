using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using AE.PID.Core.Models;
using DynamicData;

namespace AE.PID.Client.Core;

public interface IMaterialService
{
    /// <summary>
    ///     Get all material categories from the server without pagenation.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<IEnumerable<MaterialCategory>> GetCategoriesAsync();

    /// <summary>
    ///     Get the materials by category.
    /// </summary>
    /// <param name="categoryId"></param>
    /// <param name="pageRequest"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Paged<Material>> GetAsync(int? categoryId, PageRequest pageRequest,
        CancellationToken token = default);

    /// <summary>
    ///     Get the materials that matches the term.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="categoryId"></param>
    /// <param name="pageRequest"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Paged<Material>> SearchAsync(string s, int? categoryId, PageRequest pageRequest,
        CancellationToken token = default);

    /// <summary>
    ///     Get the material by its code (not by id).
    /// </summary>
    /// <param name="code"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">THe code passed in this method should not be empty.</exception>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    /// <exception cref="MaterialNotValidException">There is no valid material in the database that matches the key.</exception>
    Task<Material> GetByCodeAsync(string? code, CancellationToken token = default);

    /// <summary>
    ///     Get the category map dictionary.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Dictionary<string, string[]>> GetCategoryMapAsync();

    /// <summary>
    ///     Get the recommended materials based on material location context.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<IEnumerable<Recommendation<Material>>> GetRecommendationAsync(MaterialLocationContext context,
        CancellationToken token = default);

    /// <summary>
    ///     Send the user selection feedback to server.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="materialId"></param>
    /// <param name="collectionId"></param>
    /// <param name="recommendationId"></param>
    /// <returns></returns>
    Task FeedbackAsync(MaterialLocationContext context, int materialId, int? collectionId = null,
        int? recommendationId = null);
}