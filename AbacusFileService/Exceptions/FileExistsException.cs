namespace AbacusFileService.Exceptions;

public class FileExistsException(string message) : Exception(message);