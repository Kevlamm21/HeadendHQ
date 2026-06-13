namespace HeadendHQ.Core.Shared;

public class NotFoundException(string message) : Exception(message);

public class NotFoundException<T>(string id) : NotFoundException($"{typeof(T).Name} with Id {id} not found");
