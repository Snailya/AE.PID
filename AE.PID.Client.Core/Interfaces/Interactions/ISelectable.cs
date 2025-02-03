namespace AE.PID.Client.Core;

public interface ISelectable
{
    /// <summary>
    ///     Select the shape with specified id and make it view center.
    /// </summary>
    /// <param name="ids"></param>
    /// <exception cref="ItemNotFoundException">If there is no shape that matches the id.</exception>
    void Select(ICompoundKey[] ids);
}