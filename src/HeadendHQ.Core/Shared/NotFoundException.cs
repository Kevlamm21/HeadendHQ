namespace HeadendHQ.Core.Shared;

public abstract class NotFoundException(string message) : Exception(message);

public class NotFoundException<T>(string id) : NotFoundException($"{typeof(T).Name} with Id {id} not found");
