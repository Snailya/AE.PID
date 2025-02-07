using AE.PID.Core.DTOs;

namespace AE.PID.Server.Core;

public interface IMaterialService
{
    /// <summary>
    ///     获取满足条件的物料，并以平铺的形式表示结果。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="category"></param>
    /// <param name="nameKeyword"></param>
    /// <param name="pageNo"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    Task<IEnumerable<MaterialDto>?> GetFlattenMaterialsAsync(string userId, string? category, string? nameKeyword,
        int pageNo, int pageSize);

    /// <summary>
    ///     获取满足条件的物料，并以分页的形式表示结果。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="category"></param>
    /// <param name="nameKeyword"></param>
    /// <param name="pageNo"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    Task<Paged<MaterialDto>?> GetMaterialsAsync(string userId, string? category, string? nameKeyword, int pageNo,
        int pageSize);

    /// <summary>
    ///     通过物料编号查找物料。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    Task<MaterialDto?> GetMaterialByCodeAsync(string userId, string code);

    /// <summary>
    ///     获取满足条件的物料的数量。
    /// </summary>
    /// <param name="name"></param>
    /// <param name="code"></param>
    /// <param name="model"></param>
    /// <param name="category"></param>
    /// <param name="brand"></param>
    /// <param name="specifications"></param>
    /// <param name="manufacturer"></param>
    /// <returns></returns>
    Task<int> GetMaterialsCountAsync(string name, string code, string model, string category, string brand,
        string specifications, string manufacturer);

    /// <summary>
    ///     通过物料Id查找物料。
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<MaterialDto?> GetMaterialByIdAsync(string userId, int id);

    Task<IEnumerable<MaterialCategoryDto>> GetCategories(string userId,
        string? name = null);
}