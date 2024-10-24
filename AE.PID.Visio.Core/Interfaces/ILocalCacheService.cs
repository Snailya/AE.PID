using AE.PID.Visio.Core.Models;
using AE.PID.Visio.Core.Models.Projects;

namespace AE.PID.Visio.Core.Interfaces;

/// <summary>
///     This service manage the data used by the program, reading the history data and save the newly built data.
///     The newly used data will be saved through this class either by period or before disposed, so that the program could
///     still have limited feature if lost network connect.
/// </summary>
public interface ILocalCacheService : IStore
{
    /// <summary>
    ///     Get the material from the local storage that matches the code property.
    ///     Note that when get from web api, it should throw exception if there is no matched result, but return null here. The
    ///     logical is different because the local storage is by normal incomplete, so return the null does not mean the code
    ///     is invalid but means it is
    ///     undetermined.
    /// </summary>
    /// <param name="code"></param>
    /// <returns>null if there is no local saved data that matches the code.</returns>
    /// <exception cref="ArgumentNullException">The code passed in this method should not be empty.</exception>
    Material? GetMaterialByCode(string code);

    /// <summary>
    ///     Save the instance to document's solution xml
    /// </summary>
    /// <param name="keyword"></param>
    /// <param name="items"></param>
    /// <param name="keySelector"></param>
    /// <param name="overwrite"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    void PersistAsSolutionXml<TObject, TKey>(string keyword, TObject[] items,
        Func<TObject, TKey> keySelector, bool overwrite = false)
        where TKey : notnull;

    /// <summary>
    ///     Get the materials that saved in local storage.
    /// </summary>
    /// <returns></returns>
    IEnumerable<Material> GetMaterials();

    /// <summary>
    ///     Get the project from the local storage that mates the code property.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Project? GetProjectById(int id);
}