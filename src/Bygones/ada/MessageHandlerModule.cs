// using WinstonPuckett.PipeExtensions;
//
// namespace Elaway.Platform.EventSourcing;
//
// public delegate Task MessageHandler(object message, CancellationToken cancellationToken);
//
// [PublicAPI]
// public abstract class MessageHandlerModule {
//     internal Dictionary<Type, Func<object, CancellationToken, ValueTask<object>>> Handlers { get; } = new();
//
//     protected void On<T>(Func<T, CancellationToken, Task> when) => Handlers.Add(typeof(T), (message, ct) => when((T)message, ct));
//
//     public bool CanHandle(Type type)      => Handlers.ContainsKey(type);
//     public bool CanHandle(object message) => CanHandle(message.GetType());
//
//     public Task Handle(object message, CancellationToken cancellationToken) => Handlers[message.GetType()](message, cancellationToken);
// }
//
// public class CommandContext<T> {
//     public CommandContext(T command, Metadata metadata, IEntityStore store, CancellationToken cancellationToken) {
//         Command           = command;
//         Metadata          = metadata;
//         Store             = store;
//         CancellationToken = cancellationToken;
//     }
//
//     public T                 Command           { get; }
//     public Metadata          Metadata          { get; }
//     public IEntityStore      Store             { get; }
//     public CancellationToken CancellationToken { get; }
// }
//
// public abstract class ApplicationService<T>
//     where T : EventSourcedEntity, new() {
//  
//     protected ApplicationService(IEntityStore entities) => Entities = entities;
//
//     IEntityStore Entities { get; }
//
//
//     Dictionary<Type, Func<object, CancellationToken, Task>> Handlers { get; } = new();
//     
//     public bool CanHandle(Type type)      => Handlers.ContainsKey(type);
//     public bool CanHandle(object message) => CanHandle(message.GetType());
//
//     //public Task Handle(object message, CancellationToken cancellationToken) => Handlers[message.GetType()](message, cancellationToken);
//
//     protected void On<T>(Func<T, CancellationToken, Task> when) => Handlers.Add(typeof(T), (message, ct) => when((T)message, ct));
//
//
//
//     public Task<(long NextExpectedVersion, ulong? StreamPosition, object[] Events)> Execute<TCommand>(
//         CommandContext<TCommand> context, CancellationToken cancellationToken
//     ) {
//         var handler = Handlers[typeof(T)];
//         var task    = (Task<(long NextExpectedVersion, ulong? StreamPosition, object[] Events)>)handler(context, cancellationToken);
//         return task;
//     }
//
//     
//     
//     
//     
//     
//     protected void OnNew<TCommand>(
//         Func<TCommand, string> getEntityId,
//         Func<T, TCommand, CancellationToken, Task> handler
//     ) =>
//         On<TCommand>(
//             async (cmd, ct) => {
//                 var entityId = getEntityId(cmd);
//                 
//                 var entity = EventSourcedEntity.New<T>(entityId);
//                     
//                 await handler(entity, cmd, ct).ConfigureAwait(false);
//
//                 await Entities
//                     .Save(entity, new Metadata(), ct)
//                     .ConfigureAwait(false);
//             }
//         );
//     
//     protected void On<TCommand>(
//         Func<TCommand, string> getEntityId,
//         Func<T, TCommand, CancellationToken, Task> handler
//     ) where TCommand : EventSourcedEntity, new() =>
//         On<TCommand>(
//             async (cmd, ct) => {
//                 var entityId = getEntityId(cmd);
//                 
//                 var entity = await Entities
//                     .Load<T>(entityId, ct)
//                     .ConfigureAwait(false);
//
//                 if (entity.IsNew)
//                     throw new EntityNotFound<T>(entityId);
//
//                 await handler(entity, cmd, ct).ConfigureAwait(false);
//
//                 var result = await Entities
//                     .Save(entity, new Metadata(), ct)
//                     .ConfigureAwait(false);
//
//                 return (
//                     result.NextExpectedVersion, 
//                     result.StreamPosition, 
//                     result.Events
//                 ); 
//             }
//         );
//
//     // protected void Pipe<TCommand>(
//     //     Func<TCommand, CancellationToken, Task> validate,
//     //     Func<TCommand, string> getEntityId,
//     //     Func<T, CommandContext<T>, CancellationToken, Task> handle
//     // ) =>
//     //     On<TCommand>(
//     //         async (command, cancellationToken) => {
//     //
//     //             var wow = await command
//     //                 .Validate(validate)
//     //                 .GetEntityId(getEntityId)
//     //                 .Execute(Entities, handle);
//     //         }
//     //     );
//     
//     //     protected void Pipe<TCommand>(
//     //     Func<TCommand, CancellationToken, Task> validate,
//     //     Func<TCommand, string> getEntityId,
//     //     Func<T, TCommand, CancellationToken, Task> handle
//     // ) =>
//     //     On<TCommand>(
//     //         async (command, cancellationToken) => {
//     //
//     //             var wow = await command
//     //                 .Validate(validate)
//     //                 .GetEntityId(getEntityId)
//     //                 .Execute(Entities, handle);
//     //             
//     //             // var the_end = await command
//     //             //     .PipeAsync(
//     //             //         async (cmd, ct) => {
//     //             //             await validate(cmd, ct).ConfigureAwait(false);
//     //             //             return cmd;
//     //             //         },
//     //             //         cancellationToken
//     //             //     )
//     //             //     .PipeAsync(
//     //             //         (cmd, _) => {
//     //             //             var entityId = getEntityId(cmd);
//     //             //             return Task.FromResult((cmd, entityId));
//     //             //         },
//     //             //         cancellationToken
//     //             //     )
//     //             //     .PipeAsync(
//     //             //         async (cmd, entityId, ct) => {
//     //             //             var entity = await Entities
//     //             //                 .Load<T>(entityId, ct)
//     //             //                 .ConfigureAwait(false);
//     //             //
//     //             //             if (entity.IsNew)
//     //             //                 throw new EntityNotFound<T>(entityId);
//     //             //
//     //             //             return (cmd, entity);
//     //             //         },
//     //             //         cancellationToken
//     //             //     )
//     //             //     .PipeAsync(
//     //             //         async (cmd, entity, ct) => {
//     //             //             await handle(entity, cmd, ct).ConfigureAwait(false);
//     //             //             return entity;
//     //             //         },
//     //             //         cancellationToken
//     //             //     )
//     //             //     .PipeAsync(
//     //             //         async (entity, ct) => {
//     //             //             var result = await Entities
//     //             //                 .Save(entity, new Metadata(), ct)
//     //             //                 .ConfigureAwait(false);
//     //             //
//     //             //             return (
//     //             //                 result.NextExpectedVersion, 
//     //             //                 result.StreamPosition, 
//     //             //                 result.Events
//     //             //             );
//     //             //         },
//     //             //         cancellationToken
//     //             //     );
//     //         }
//     //     );
// }
//
//
//
// // public abstract class ApplicationService<T> : MessageHandlerModule
// //     where T : EventSourcedEntity, new() {
// //  
// //     protected ApplicationService(IEntityStore entities) => Entities = entities;
// //
// //     IEntityStore Entities { get; }
// //     
// //     protected void OnNew<TCommand>(
// //         Func<TCommand, string> getEntityId,
// //         Func<T, TCommand, CancellationToken, Task> handler
// //     ) =>
// //         On<TCommand>(
// //             async (cmd, ct) => {
// //                 var entityId = getEntityId(cmd);
// //                 
// //                 var entity = EventSourcedEntity.New<T>(entityId);
// //                     
// //                 await handler(entity, cmd, ct).ConfigureAwait(false);
// //
// //                 await Entities
// //                     .Save(entity, new Metadata(), ct)
// //                     .ConfigureAwait(false);
// //             }
// //         );
// //     
// //     protected void On<TCommand>(
// //         Func<TCommand, string> getEntityId,
// //         Func<T, TCommand, CancellationToken, Task> handler
// //     ) where TCommand : EventSourcedEntity, new() =>
// //         On<TCommand>(
// //             async (cmd, ct) => {
// //                 var entityId = getEntityId(cmd);
// //                 
// //                 var entity = await Entities
// //                     .Load<T>(entityId, ct)
// //                     .ConfigureAwait(false);
// //
// //                 if (entity.IsNew)
// //                     throw new EntityNotFound<T>(entityId);
// //
// //                 await handler(entity, cmd, ct).ConfigureAwait(false);
// //
// //                 var result = await Entities
// //                     .Save(entity, new Metadata(), ct)
// //                     .ConfigureAwait(false);
// //
// //                 return (
// //                     result.NextExpectedVersion, 
// //                     result.StreamPosition, 
// //                     result.Events
// //                 ); 
// //             }
// //         );
// //
// //     public  Task<(long NextExpectedVersion, ulong? StreamPosition, object[] Events)> Execute<TCommand>(CommandContext<TCommand> context, CancellationToken cancellationToken) {
// //         var handler = Handlers[typeof(T)];
// //         var task    = (Task<(long NextExpectedVersion, ulong? StreamPosition, object[] Events)>) handler(context, cancellationToken);
// //         return task;
// //     }
// //
// //     // protected void Pipe<TCommand>(
// //     //     Func<TCommand, CancellationToken, Task> validate,
// //     //     Func<TCommand, string> getEntityId,
// //     //     Func<T, CommandContext<T>, CancellationToken, Task> handle
// //     // ) =>
// //     //     On<TCommand>(
// //     //         async (command, cancellationToken) => {
// //     //
// //     //             var wow = await command
// //     //                 .Validate(validate)
// //     //                 .GetEntityId(getEntityId)
// //     //                 .Execute(Entities, handle);
// //     //         }
// //     //     );
// //     
// //     //     protected void Pipe<TCommand>(
// //     //     Func<TCommand, CancellationToken, Task> validate,
// //     //     Func<TCommand, string> getEntityId,
// //     //     Func<T, TCommand, CancellationToken, Task> handle
// //     // ) =>
// //     //     On<TCommand>(
// //     //         async (command, cancellationToken) => {
// //     //
// //     //             var wow = await command
// //     //                 .Validate(validate)
// //     //                 .GetEntityId(getEntityId)
// //     //                 .Execute(Entities, handle);
// //     //             
// //     //             // var the_end = await command
// //     //             //     .PipeAsync(
// //     //             //         async (cmd, ct) => {
// //     //             //             await validate(cmd, ct).ConfigureAwait(false);
// //     //             //             return cmd;
// //     //             //         },
// //     //             //         cancellationToken
// //     //             //     )
// //     //             //     .PipeAsync(
// //     //             //         (cmd, _) => {
// //     //             //             var entityId = getEntityId(cmd);
// //     //             //             return Task.FromResult((cmd, entityId));
// //     //             //         },
// //     //             //         cancellationToken
// //     //             //     )
// //     //             //     .PipeAsync(
// //     //             //         async (cmd, entityId, ct) => {
// //     //             //             var entity = await Entities
// //     //             //                 .Load<T>(entityId, ct)
// //     //             //                 .ConfigureAwait(false);
// //     //             //
// //     //             //             if (entity.IsNew)
// //     //             //                 throw new EntityNotFound<T>(entityId);
// //     //             //
// //     //             //             return (cmd, entity);
// //     //             //         },
// //     //             //         cancellationToken
// //     //             //     )
// //     //             //     .PipeAsync(
// //     //             //         async (cmd, entity, ct) => {
// //     //             //             await handle(entity, cmd, ct).ConfigureAwait(false);
// //     //             //             return entity;
// //     //             //         },
// //     //             //         cancellationToken
// //     //             //     )
// //     //             //     .PipeAsync(
// //     //             //         async (entity, ct) => {
// //     //             //             var result = await Entities
// //     //             //                 .Save(entity, new Metadata(), ct)
// //     //             //                 .ConfigureAwait(false);
// //     //             //
// //     //             //             return (
// //     //             //                 result.NextExpectedVersion, 
// //     //             //                 result.StreamPosition, 
// //     //             //                 result.Events
// //     //             //             );
// //     //             //         },
// //     //             //         cancellationToken
// //     //             //     );
// //     //         }
// //     //     );
// // }
//
// public static class Extensions {
//     public static Task<T> Validate<T>(this T command, Func<T, CancellationToken, Task> handler) =>
//         command.PipeAsync(
//             async (cmd, ct) => {
//                 await handler(cmd, ct).ConfigureAwait(false);
//                 return cmd;
//             }
//         );
//
//     public static Task<(T Command, string EntityId)> GetEntityId<T>(this Task<T> validate, Func<T, string> handler) {
//         return validate.PipeAsync(
//             (cmd, _) => {
//                 var entityId = handler(cmd);
//                 return Task.FromResult((Command: cmd, EntityId: entityId));
//             }
//         );
//     }
//
//     public static Task<(long NextExpectedVersion, ulong? StreamPosition, object[] Events)> Execute<TEntity, TCommand>(
//         this Task<(TCommand Command, string EntityId)> getEntityId, IEntityStore store, Func<TEntity, TCommand, CancellationToken, Task> handler
//     ) where TEntity : EventSourcedEntity, new() {
//         return getEntityId.PipeAsync(
//             async (cmd, entityId, ct) => {
//                 var entity = await store
//                     .Load<TEntity>(entityId, ct)
//                     .ConfigureAwait(false);
//
//                 if (entity.IsNew)
//                     throw new EntityNotFound<TEntity>(entityId);
//
//                 await handler(entity, cmd, ct).ConfigureAwait(false);
//
//                 var result = await store
//                     .Save(entity, new Metadata(), ct)
//                     .ConfigureAwait(false);
//
//                 return (
//                     result.NextExpectedVersion,
//                     result.StreamPosition,
//                     result.Events
//                 );
//             }
//         );
//     }
// }