namespace Orders.Api.Exceptions;

public class OrderNotFoundException : Exception { }
public class ValidationException : Exception {
    public string Field { get; }
    public ValidationException(string field, string message) : base(message) { Field = field; }
}
