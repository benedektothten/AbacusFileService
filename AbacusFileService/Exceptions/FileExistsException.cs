namespace AbacusFileService.Exceptions;

/// <summary>
/// The file is already exists exception.
/// </summary>
/// <param name="message"></param>
public class FileExistsException(string message) : Exception(message);