using AbacusFileService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AbacusFileService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController(IFileService fileService) : Controller
{
    /// <summary>
    /// Returns a list of all files in the storage container.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>List of strings</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var files = await fileService.ListAsync(cancellationToken);
        return Ok(files);
    }

    /// <summary>
    /// Returns a URL with SAS token to download the file.
    /// </summary>
    /// <param name="blobName">The name of the file</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Returns a url</returns>
    [HttpGet("{blobName}")]
    public async Task<IActionResult> Download(string blobName, CancellationToken cancellationToken)
    {
        return Ok(await fileService.DownloadAsync(blobName, cancellationToken));
    }
    
    /// <summary>
    /// Uploads a file to the storage container.
    /// </summary>
    /// <param name="file">File in the request</param>
    /// <param name="blobName">Name of the file to save, optional, if left empty the service will use the name of the file.</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Returns 201 if the upload was successful</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(2L * 1024 * 1024 * 1024)] // 2 GB
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, [FromForm] string? blobName, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest("File is required.");
        var name = string.IsNullOrWhiteSpace(blobName) ? file.FileName : blobName;

        await using var stream = file.OpenReadStream();
        await fileService.UploadAsync(name, stream, file.ContentType, cancellationToken).ConfigureAwait(false);

        return CreatedAtAction(nameof(Download), new { blobName = name }, null);
    }
    


    /// <summary>
    /// Deletes a file from the storage container.
    /// </summary>
    /// <param name="blobName">The name of the file to be deleted</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Returns 204 if the deletion was successful, returns 404 if the is not exists</returns>
    [HttpDelete("{blobName}")]
    public async Task<IActionResult> Delete(string blobName, CancellationToken cancellationToken)
    {
        var deleted = await fileService.DeleteAsync(blobName, cancellationToken).ConfigureAwait(false);
        if (!deleted) return NotFound();
        return NoContent();
    }
}