namespace Bygones;

public abstract class DomainException : Exception {
    protected DomainException(string message, Exception? inner = null) 
        : base(message, inner) { }

    public Guid Id { get; } = Guid.NewGuid();
}

public class InvalidEntityId : DomainException {
    public InvalidEntityId(string entityId, Type entityType, Exception? inner = null) 
        : base($"{entityType.Name} id is invalid", inner) { }
}

public class EntityNotFound : DomainException {
    public EntityNotFound(string entityId, Type entityType, Exception? inner = null)
        : base($"{entityType.Name}:{entityId} not found", inner) { }
}

//
// public class InvalidCommandException : InvalidOperationException {
//     protected InvalidCommandException(object command)
//         : base($"{command.GetType().Name} command is invalid") {
//         Command = command;
//     }
//
//     public object Command { get; }
// }
//
// public class CommandHandlerNotFoundException : InvalidOperationException {
//     public CommandHandlerNotFoundException(object command)
//         : base($"{command.GetType().Name} handler not found") {
//         Command = command;
//     }
//
//     public object Command { get; }
// }
