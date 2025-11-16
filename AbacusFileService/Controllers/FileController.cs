using AbacusFileService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AbacusFileService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController(IFileService fileService) : Controller
{
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, [FromForm] string? blobName, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest("File is required.");
        var name = string.IsNullOrWhiteSpace(blobName) ? file.FileName : blobName;

        await using var stream = file.OpenReadStream();
        await fileService.UploadAsync(name, stream, file.ContentType, cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(nameof(Download), new { blobName = name }, null);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var files = await fileService.ListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(files);
    }

    // Read (download)
    [HttpGet("{blobName}")]
    public async Task<IActionResult> Download(string blobName, CancellationToken cancellationToken)
    {
        return Ok(await fileService.DownloadAsync(blobName, cancellationToken).ConfigureAwait(false));
    }

    // Delete
    [HttpDelete("{blobName}")]
    public async Task<IActionResult> Delete(string blobName, CancellationToken cancellationToken)
    {
        var deleted = await fileService.DeleteAsync(blobName, cancellationToken).ConfigureAwait(false);
        if (!deleted) return NotFound();
        return NoContent();
    }
}