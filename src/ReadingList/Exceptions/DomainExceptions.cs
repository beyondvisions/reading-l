namespace ReadingList.Exceptions;

public class NotFoundException : AppException
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Title => "Resource not found";

    public NotFoundException(string message) : base(message) { }
}

public class ConflictException : AppException
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Title => "Conflict";

    public ConflictException(string message) : base(message) { }
}

public class UnauthorizedException : AppException
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public override string Title => "Unauthorized";

    public UnauthorizedException(string message) : base(message) { }
}

public class ForbiddenException : AppException
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public override string Title => "Forbidden";

    public ForbiddenException(string message) : base(message) { }
}

public class BadRequestException : AppException
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public override string Title => "Bad request";

    public BadRequestException(string message) : base(message) { }
}