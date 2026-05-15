using Csla;
using Ossendorf.Csla.PollingCommand.Client;

namespace Ossendorf.Csla.PollingCommand;

/// <summary>
/// Defines a mechanism for executing long running commands with polling.
/// </summary>
public interface IPollingCommand {
    /// <summary>
    /// Executes the <typeparamref name="T"/> command without any parameters.
    /// </summary>
    /// <typeparam name="T">Command to execute.</typeparam>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task<T> Execute<T>() where T : CommandBase<T>;

    /// <summary>
    /// Executes the <typeparamref name="T"/> command without any parameters.
    /// </summary>
    /// <typeparam name="T">Command to execute.</typeparam>
    /// <param name="options">The options to configure per execution options.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    Task<T> Execute<T>(PollingOptions options) where T : CommandBase<T>;

    /// <summary>
    /// Executes the <typeparamref name="T"/> command wit the given parameters.
    /// </summary>
    /// <typeparam name="T">Command to execute.</typeparam>
    /// <param name="executeParameters">The parameters for the execute method. Remark: Make sure only to pass objects which are serializable by csla.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executeParameters"/> is <see langword="null"/>.</exception>
    Task<T> Execute<T>(params object[] executeParameters) where T : CommandBase<T>;

    /// <summary>
    /// Executes the <typeparamref name="T"/> command wit the given parameters.
    /// </summary>
    /// <typeparam name="T">Command to execute.</typeparam>
    /// <param name="options">The options to configure per execution options.</param>
    /// <param name="executeParameters">The parameters for the execute method. Remark: Make sure only to pass objects which are serializable by csla.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="executeParameters"/> is <see langword="null"/>.</exception>
    Task<T> Execute<T>(PollingOptions options, params object[] executeParameters) where T : CommandBase<T>;
}