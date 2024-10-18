using AE.PID.Core.DTOs;
using AE.PID.Visio.Core.Exceptions;
using AE.PID.Visio.Core.Models;
using DynamicData;

namespace AE.PID.Visio.Core.Interfaces;

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
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Material?> GetByCodeAsync(string code, CancellationToken token = default);

    /// <summary>
    ///     Get the category map dictionary.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NetworkNotValidException">There is a network error between server and local.</exception>
    Task<Dictionary<string, string[]>> GetCategoryMapAsync();
}