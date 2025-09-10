using System;

namespace Rail.Indexing;

/// <summary>
/// Исключение для ошибок валидации индекса поезда
/// </summary>
public class TrainIndexValidationException : Exception
{
    public TrainIndexValidationException(string message) : base(message) { }
    public TrainIndexValidationException(string message, Exception innerException) : base(message, innerException) { }
}