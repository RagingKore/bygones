// namespace Elaway.Platform.EventSourcing;
//
// public abstract class ApplicationService<T>
//     where T : EventSourcedEntity {
//     
//     protected IEntityStore Store { get; }
//
//     readonly HandlersMap<T> _handlers = new();
//
//     protected ApplicationService(IEntityStore store) {
//         Store = store;
//     }
//
//     // /// <summary>
//     // /// Register a handler for a command, which is expected to create a new aggregate instance.
//     // /// </summary>
//     // /// <param name="action">Action to be performed on the aggregate, given the aggregate instance and the command</param>
//     // /// <typeparam name="TCommand">Command type</typeparam>
//     // protected void OnNew<TCommand>(ActOnAggregate<TCommand> action)
//     //     where TCommand : class => _handlers.Add(
//     //     typeof(TCommand),
//     //     new RegisteredHandler<T>(
//     //         ExpectedState.New,
//     //         (aggregate, cmd, _) => SyncAsTask(aggregate, cmd, action)
//     //     )
//     // );
//     //
//     // /// <summary>
//     // /// Register an asynchronous handler for a command, which is expected to create a new aggregate instance.
//     // /// </summary>
//     // /// <param name="action">Asynchronous action to be performed on the aggregate,
//     // /// given the aggregate instance and the command</param>
//     // /// <typeparam name="TCommand">Command type</typeparam>
//     // protected void OnNewAsync<TCommand>(ActOnEntityAsync<TCommand> action)
//     //     where TCommand : class => _handlers.Add(
//     //     typeof(TCommand),
//     //     new RegisteredHandler<T>(
//     //         ExpectedState.New,
//     //         (aggregate, cmd, ct) => AsTask(aggregate, cmd, action, ct)
//     //     )
//     // );
//     //
//     // /// <summary>
//     // /// Register a handler for a command, which is expected to use an existing aggregate instance.
//     // /// </summary>
//     // /// <param name="getEntityId">A function to get the aggregate id from the command</param>
//     // /// <param name="action">Action to be performed on the aggregate, given the aggregate instance and the command</param>
//     // /// <typeparam name="TCommand">Command type</typeparam>
//     // protected void OnExisting<TCommand>(
//     //     GetIdFromCommand<TCommand> getEntityId,
//     //     ActOnAggregate<TCommand>   action
//     // )
//     //     where TCommand : class {
//     //     _handlers.Add(
//     //         typeof(TCommand),
//     //         new RegisteredHandler<T>(
//     //             ExpectedState.Existing,
//     //             (aggregate, cmd, _) => SyncAsTask(aggregate, cmd, action)
//     //         )
//     //     );
//     //
//     //     //_getId.TryAdd(typeof(TCommand), (cmd, _) => new ValueTask<TId>(getEntityId((TCommand)cmd)));
//     // }
//     //
//     // /// <summary>
//     // /// Register an asynchronous handler for a command, which is expected to use an existing aggregate instance.
//     // /// </summary>
//     // /// <param name="getEntityId">A function to get the aggregate id from the command</param>
//     // /// <param name="action">Asynchronous action to be performed on the aggregate,
//     // /// given the aggregate instance and the command</param>
//     // /// <typeparam name="TCommand">Command type</typeparam>
//     // protected void OnExistingAsync<TCommand>(
//     //     GetIdFromCommand<TCommand>    getEntityId,
//     //     ActOnEntityAsync<TCommand> action
//     // )
//     //     where TCommand : class {
//     //     _handlers.Add(
//     //         typeof(TCommand),
//     //         new RegisteredHandler<T>(
//     //             ExpectedState.Existing,
//     //             (aggregate, cmd, ct) => AsTask(aggregate, cmd, action, ct)
//     //         )
//     //     );
//     //
//     //     //_getId.TryAdd(typeof(TCommand), (cmd, _) => new ValueTask<TId>(getEntityId((TCommand)cmd)));
//     // }
//
//     /// <summary>
//     /// Register a handler for a command, which is expected to use an a new or an existing aggregate instance.
//     /// </summary>
//
//     protected void On<TCommand>(
//         Func<TCommand, string> getEntityId,
//         ActOnEntity<TCommand>   action
//     ) {
//         _handlers.Add(
//             typeof(TCommand),
//             new RegisteredHandler<T>(
//                 ExpectedState.Any,
//                 (entity, cmd, _) => SyncAsTask(entity, cmd, action)
//             )
//         );
//
//         //_getId.TryAdd(typeof(TCommand), (cmd, _) => new ValueTask<TId>(getEntityId((TCommand)cmd)));
//     }
//
//     /// <summary>
//     /// Register an asynchronous handler for a command, which is expected to use an a new or an existing aggregate instance.
//     /// </summary>
//     /// <param name="getId">A function to get the aggregate id from the command</param>
//     /// <param name="action">Asynchronous action to be performed on the aggregate,
//     /// given the aggregate instance and the command</param>
//     /// <typeparam name="TCommand">Command type</typeparam>
//     protected void On<TCommand>(
//         Func<TCommand, string> getId,
//         ActOnEntityAsync<TCommand> action
//     )
//         where TCommand : class {
//         _handlers.Add(
//             typeof(TCommand),
//             new RegisteredHandler<T>(
//                 ExpectedState.Any,
//                 (aggregate, cmd, ct) => AsTask(aggregate, cmd, action, ct)
//             )
//         );
//
//        // _getId.TryAdd(typeof(TCommand), (cmd, _) => new ValueTask<TId>(getEntityId((TCommand)cmd)));
//     }
//
//     static ValueTask<T> SyncAsTask<TCommand>(
//         T                        aggregate,
//         object                   cmd,
//         ActOnAggregate<TCommand> action
//     ) {
//         action(aggregate, (TCommand)cmd);
//         return new ValueTask<T>(aggregate);
//     }
//
//     static async ValueTask<T> AsTask<TCommand>(
//         T                             aggregate,
//         object                        cmd,
//         ActOnEntityAsync<TCommand> action,
//         CancellationToken             cancellationToken
//     ) {
//         await action(aggregate, (TCommand)cmd, cancellationToken).ConfigureAwait(false);
//         return aggregate;
//     }
//
//     /// <summary>
//     /// The generic command handler. Call this function from your edge (API).
//     /// </summary>
//     public async Task<Result<TState, TId>> Execute<TCommand>(
//         TCommand          command,
//         CancellationToken cancellationToken
//     )
//         where TCommand : class {
//         if (!_handlers.TryGetValue(typeof(TCommand), out var registeredHandler)) {
//             throw new CommandHandlerNotFoundException(typeof(TCommand));
//         }
//
//         var aggregate = registeredHandler.ExpectedState switch {
//             ExpectedState.Any      => await TryLoad().NoContext(),
//             ExpectedState.Existing => await Load().NoContext(),
//             ExpectedState.New      => Create(),
//             ExpectedState.Unknown  => default,
//             _ => throw new ArgumentOutOfRangeException(
//                 nameof(registeredHandler.ExpectedState),
//                 "Unknown expected state"
//             )
//         };
//
//         var result = await registeredHandler.Handler(aggregate!, command, cancellationToken)
//             .NoContext();
//
//         var storeResult = await Store.Store(result, cancellationToken).NoContext();
//
//         return new OkResult<TState, TId>(result.State, result.Changes, storeResult.GlobalPosition);
//
//         async Task<T> Load() {
//             var id = await _getId[typeof(TCommand)](command, cancellationToken).NoContext();
//             return await Store.Load<T, TState, TId>(id, cancellationToken).NoContext();
//         }
//
//         async Task<T> TryLoad() {
//             var id     = await _getId[typeof(TCommand)](command, cancellationToken).NoContext();
//             var exists = await Store.Exists<T>(id, cancellationToken);
//             return exists ? await Load().NoContext() : Create();
//         }
//
//         T Create() => _factoryRegistry.CreateInstance<T, TState, TId>();
//     }
//
//     public delegate Task ActOnEntityAsync<in TCommand>(
//         T                 entity,
//         TCommand          command,
//         CancellationToken cancellationToken
//     );
//
//     public delegate void ActOnEntity<in TCommand>(T entity, TCommand command);
//
//     // public delegate Task<T> ArbitraryActAsync<in TCommand>(
//     //     TCommand          command,
//     //     CancellationToken cancellationToken
//     // );
//
//     // public delegate TId GetIdFromCommand<in TCommand>(TCommand command);
//
//     // public delegate Task<TId> GetIdFromCommandAsync<in TCommand>(
//     //     TCommand          command,
//     //     CancellationToken cancellationToken
//     // );
//
//     // async Task<Result> IApplicationService<T>.Handle<TCommand>(TCommand command, CancellationToken cancellationToken) {
//     //     var (state, enumerable) = await Handle(command, cancellationToken);
//     //     return new Result(state, enumerable);
//     // }
// }
//
// record RegisteredHandler<T>(
//     ExpectedState                                    ExpectedState,
//     Func<T, object, CancellationToken, ValueTask<T>> Handler
// );
//
// class HandlersMap<T> : Dictionary<Type, RegisteredHandler<T>> { }
//
// enum ExpectedState {
//     New,
//     Existing,
//     Any
// }